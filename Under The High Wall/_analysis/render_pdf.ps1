param(
  [string]$PdfPath = "C:\Users\EDY\Desktop\Portfolio_LuoZhiyang.pdf",
  [int[]]$Pages = @(12,13,14,15,16),
  [string]$OutDir = "D:\Github\Under-The-High-Wall\Under The High Wall\_analysis\pdf_pages"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Add-Type -AssemblyName System.Runtime.WindowsRuntime

$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
})[0]

function Await($WinRtTask, $ResultType) {
    $asTask = $asTaskGeneric.MakeGenericMethod($ResultType)
    $netTask = $asTask.Invoke($null, @($WinRtTask))
    $netTask.Wait(-1) | Out-Null
    $netTask.Result
}

function AwaitAction($WinRtAction) {
    $netTask = [System.WindowsRuntimeSystemExtensions]::AsTask($WinRtAction)
    $netTask.Wait(-1) | Out-Null
}

[Windows.Data.Pdf.PdfDocument, Windows.Data.Pdf, ContentType = WindowsRuntime] | Out-Null
[Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime] | Out-Null

$file = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($PdfPath)) ([Windows.Storage.StorageFile])
$pdf = Await ([Windows.Data.Pdf.PdfDocument]::LoadFromFileAsync($file)) ([Windows.Data.Pdf.PdfDocument])
Write-Output ("PAGECOUNT=" + $pdf.PageCount)

foreach ($p in $Pages) {
    if ($p -lt 1 -or $p -gt $pdf.PageCount) { continue }
    $page = $pdf.GetPage($p - 1)
    $outFile = Join-Path $OutDir ("page_{0:D2}.png" -f $p)
    if (Test-Path $outFile) { Remove-Item $outFile -Force }
    New-Item -ItemType File -Path $outFile | Out-Null
    $storageFile = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($outFile)) ([Windows.Storage.StorageFile])
    $stream = Await ($storageFile.OpenAsync([Windows.Storage.FileAccessMode]::ReadWrite)) ([Windows.Storage.Streams.IRandomAccessStream])
    $opts = New-Object Windows.Data.Pdf.PdfPageRenderOptions
    $opts.DestinationWidth = [uint32][math]::Round($page.Size.Width * 2.2)
    $opts.DestinationHeight = [uint32][math]::Round($page.Size.Height * 2.2)
    AwaitAction ($page.RenderToStreamAsync($stream, $opts))
    $stream.Dispose()
    $page.Dispose()
    Write-Output ("WROTE " + $outFile)
}

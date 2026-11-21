$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
})[0]
function Await($WinRtTask, $ResultType) {
    $asTask = $asTaskGeneric.MakeGenericMethod($ResultType)
    $netTask = $asTask.Invoke($null, @($WinRtTask))
    try { $netTask.Wait(-1) | Out-Null } catch {
        Write-Output "WAIT ERR: $($_.Exception.Message)"
        Write-Output "INNER: $($_.Exception.InnerException)"
        throw
    }
    $netTask.Result
}
[Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Pdf.PdfDocument, Windows.Data.Pdf, ContentType = WindowsRuntime] | Out-Null
$f = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync("C:\Users\EDY\Desktop\Portfolio_LuoZhiyang.pdf")) ([Windows.Storage.StorageFile])
Write-Output ("FILE OK " + $f.Name + " " + $f.Path)
$pdf = Await ([Windows.Data.Pdf.PdfDocument]::LoadFromFileAsync($f)) ([Windows.Data.Pdf.PdfDocument])
Write-Output ("PAGECOUNT " + $pdf.PageCount)

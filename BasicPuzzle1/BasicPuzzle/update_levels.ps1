$content = Get-Content "c:/Unity proje/BasicPuzzle/Assets/Resources/levels.txt"
$newContent = @()
foreach ($line in $content) {
    $trimmed = $line.Trim()
    if ($trimmed -like "#*" -or $trimmed -eq "") {
        $newContent += $line
        continue
    }
    $parts = $trimmed.Split("|")
    if ($parts.Length -lt 5) {
        $newContent += $line
        continue
    }
    $levelNo = [int]::Parse($parts[0].Trim())
    $layout = "Grid"
    if ($levelNo -eq 1 -or $levelNo -eq 2 -or $levelNo -eq 5 -or $levelNo -eq 8) {
        $layout = "Grid"
    } elseif ($levelNo -eq 3 -or $levelNo -eq 6 -or $levelNo -eq 9) {
        $layout = "Pyramid"
    } elseif ($levelNo -eq 4 -or $levelNo -eq 7 -or $levelNo -eq 10) {
        $layout = "InversePyramid"
    } else {
        $mod = ($levelNo - 11) % 5
        switch ($mod) {
            0 { $layout = "Diamond" }
            1 { $layout = "Circle" }
            2 { $layout = "Grid" }
            3 { $layout = "Pyramid" }
            4 { $layout = "InversePyramid" }
        }
    }
    
    $outParts = @()
    for ($i = 0; $i -lt 8; $i++) {
        if ($i -lt $parts.Length) {
            $outParts += $parts[$i].Trim()
        } else {
            if ($i -eq 5) { $outParts += "150" }
            elseif ($i -eq 6) { $outParts += "0" }
            elseif ($i -eq 7) { $outParts += "0" }
        }
    }
    $outParts += $layout
    $newContent += ($outParts -join " | ")
}
$newContent | Out-File -FilePath "c:/Unity proje/BasicPuzzle/Assets/Resources/levels.txt" -Encoding utf8

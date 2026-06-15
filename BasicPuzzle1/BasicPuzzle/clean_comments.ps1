$path = "c:/Unity proje/BasicPuzzle/Assets/Resources/levels.txt"
$text = Get-Content -Path $path -Raw -Encoding utf8

$replacements = @{
    "AÅžAMA" = "ASAMA"
    "BAÅžLANGIÃ‡ VE ISINMA" = "BASLANGIC VE ISINMA"
    "SÄ±ralÄ± aÃ§Ä±lÄ±ÅŸ" = "Sirali acilis"
    "gÃ¶sterme sÃ¼resi" = "gosterme suresi"
    "HÄ±zlÄ±" = "Hizli"
    "KarÄ±ÅŸÄ±k aÃ§Ä±lÄ±ÅŸ" = "Karisik acilis"
    "TuzakZarfSayÄ±sÄ±" = "TuzakZarfSayisi"
    "BaÅŸlangÄ±Ã§KonumunuKarÄ±ÅŸtÄ±r" = "BaslangicKonumunuKaristir"
    "KarÄ±ÅŸtÄ±rmaSayÄ±sÄ±" = "KaristirmaSayisi"
    "HÄ±z" = "Hiz"
    "CÃ¼mle" = "Cumle"
    "BeklemeSÃ¼resi" = "BeklemeSuresi"
    "GÃ¶sterme" = "Gosterme"
    "SatÄ±r baÅŸÄ±nda" = "Satir basinda"
    "gÃ¶z ardÄ± edilir" = "goz ardi edilir"
    "yÃ¼klenirken" = "yuklenirken"
    "TEMPOYU ARTIRIYORUZ" = "TEMPOYU ARTIRIYORUZ"
    "TUZAKLAR DEVREYE GÄ°RÄ°YOR" = "TUZAKLAR DEVREYE GIRIYOR"
    "Ä°LERÄ° SEVÄ°YE HAFIZA TESTÄ°" = "ILERI SEVIYE HAFIZA TESTI"
    "KOZMÄ°K KAOS / FÄ°NAL" = "KOZMIK KAOS / FINAL"
}

foreach ($key in $replacements.Keys) {
    $text = $text.Replace($key, $replacements[$key])
}

$text | Out-File -FilePath $path -Encoding utf8 -NoBOM

namespace BasicPuzzle.Core
{
    public enum GameState
    {
        Loading,          // Seviye hazırlanıyor, zarflar konumlandırılıyor
        ShowingWords,     // Kelimeler oyuncuya ezberlemesi için açık gösteriliyor
        Shuffling,        // Zarflar kapanıp DOTween ile yer değiştiriyor
        Gameplay,         // Oyuncu sırayla doğru kelimeleri bulmaya çalışıyor
        LevelWin,         // Seviye kazanıldı
        LevelFail         // Seviye kaybedildi (varsa can/süre bitti)
    }
}

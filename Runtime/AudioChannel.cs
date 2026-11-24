namespace Atthree.Audio
{
    /// <summary>
    /// Ses kanalları. Her kanalın ses seviyesi ayrı ayarlanır (ayarlar menüsündeki kaydırıcılar).
    /// Master hepsini birden etkiler; bir ses Master kanalında çalınamaz.
    /// </summary>
    public enum AudioChannel
    {
        Master,
        Music,
        Sfx,
        Ui,
        Voice,
        Ambience,
    }
}

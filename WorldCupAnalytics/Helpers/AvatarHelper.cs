namespace WorldCupAnalytics.Helpers
{
    // Generates avatar image URLs via DiceBear's free public API — these
    // are algorithmically generated from the name (colored initials),
    // NOT real photos. Real player headshots are copyrighted and aren't
    // something we can legally source/host, so this is the practical
    // stand-in: a distinct, decent-looking image per player, for free,
    // with no licensing concerns.
    public static class AvatarHelper
    {
        // hexColor should be WITHOUT the leading '#', e.g. "2E7D50"
        public static string GetUrl(string playerName, string hexColor)
        {
            var seed = Uri.EscapeDataString(playerName);
            var bg = hexColor.TrimStart('#');
            return $"https://api.dicebear.com/9.x/initials/svg?seed={seed}&backgroundColor={bg}&fontFamily=Arial&fontWeight=600";
        }
    }
}
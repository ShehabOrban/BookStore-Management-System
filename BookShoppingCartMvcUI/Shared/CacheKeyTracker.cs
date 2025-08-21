namespace BookShoppingCartMvcUI.Shared
{
    public static class CacheKeyTracker
    {
        public static HashSet<string> Keys { get; } = new HashSet<string>();
    }
}

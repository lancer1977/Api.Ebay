namespace PolyhydraGames.API.Ebay
{
    public static class Scopes
    {
        public const string Prefix = "https://api.ebay.com/oauth/api_scope/";
        public const string Buy_Order_Readonly = Prefix + "buy.order.readonly";
        public const string Buy_Guest_Order = Prefix + "buy.guest.order";
        public const string Sell_Account = Prefix + "sell.account";
        public const string Sell_Inventory = Prefix + "sell.inventory";
    }
}
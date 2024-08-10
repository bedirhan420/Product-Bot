#region Usings
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
#endregion

#region Classes
public class Product
{
    public ulong Id = 0;
    public string Name = "";
    public string Image = "";
    public int MinPrice = 0;
    public bool isSaled = false;

    public Product() 
    {
        string hexGuid = Guid.NewGuid().ToString("N").Substring(0, 16);
        if (hexGuid.Length > 16)
        {
            hexGuid = hexGuid.Substring(0, 16);
        }

        Id = ulong.Parse(hexGuid, System.Globalization.NumberStyles.HexNumber);
    }


}
public class Seller
{
    public string UserId = "";
    public string UserName = "";
    public List<Product> Products = new List<Product>();
    public static void SaveAllToJson(List<Seller> sellers, string filePath)
    {
        try
        {
            string json = JsonConvert.SerializeObject(sellers, Formatting.Indented);
            File.WriteAllText(filePath, json);
            Console.WriteLine($"Sellers saved to {filePath} successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving sellers to {filePath}: {ex.Message}");
        }
    }
}
public class Buyer
{
    public ulong UserId = 0;
    public Buyer(ulong userId)
    {
        UserId= userId;
    }
}
public class Anonim
{
    public ulong Id = 0;
    public Seller Seller;
    public Buyer Buyer;
    public Product Product;

    public  bool SellerAprove=false;
    public  bool BuyerAprove=false;
    public Anonim(Seller seller,Buyer buyer,Product product)
    {
        string hexGuid = Guid.NewGuid().ToString("N").Substring(0, 16);
        if (hexGuid.Length > 16)
        {
            hexGuid = hexGuid.Substring(0, 16);
        }
        Seller = seller;
        Buyer = buyer;
        Product = product;
        Id = ulong.Parse(hexGuid, System.Globalization.NumberStyles.HexNumber);
    }
    public static void SaveAllToJson(List<Anonim> anonims, string filePath)
    {
        try
        {
            string json = JsonConvert.SerializeObject(anonims, Formatting.Indented);
            File.WriteAllText(filePath, json);
            Console.WriteLine($"Anonims saved to {filePath} successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving anonims to {filePath}: {ex.Message}");
        }
    }
}
#endregion

namespace Proje
{
    internal class Program
    { 
        #region Variables
        public static DiscordSocketClient _client;
        public static string botToken = "";
        public static ulong guildId = 0;
        public static ulong productsChannelId =LoadPrdouctsChannelIdFromJson("productsChannelId.json");
        public static ulong adminsChannelId = LoadAdminsChannelIdFromJson("adminsChannelId.json");
        public static DiscordSocketConfig config = new DiscordSocketConfig();

        public static List<Seller> sellers = LoadSellersFromJson("sellers.json");
        public static List<Anonim> anonims = LoadAnonimsFromJson("anonims.json");
        public static List<Product> products = new List<Product>();
        #endregion

        #region Main
        static void Main(string[] args)
        {
            var str = File.ReadAllText("config.txt");
            botToken = (str.Split(',')[0]);
            guildId = (ulong.Parse(str.Split(',')[1]));
            MainAsync().GetAwaiter().GetResult();
        }
        public static async Task MainAsync()
        {
            try
            {
                _client = new DiscordSocketClient(config);

                _client.Log += _client_Log;
                _client.Ready += _client_Ready;
                _client.SlashCommandExecuted += _client_SlashCommandExecuted;
                _client.ButtonExecuted += _client_ButtonExecuted;
                _client.ModalSubmitted += _client_ModalSubmitted;

                await _client.LoginAsync(TokenType.Bot, botToken);
                await _client.StartAsync();

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MainAsync: {ex}");
            }
        }
        #endregion

        #region Clients
        private async static Task _client_ModalSubmitted(SocketModal arg)
        {
            try
            {
                switch (arg.Data.CustomId.Split('#')[0].ToString())
                {
                    case "update":
                        await updateProduct(arg,arg.Data.CustomId.Split('#')[1], ulong.Parse(arg.Data.CustomId.Split('#')[2]), int.Parse(arg.Data.CustomId.Split('#')[3]), int.Parse(arg.Data.CustomId.Split('#')[4]), int.Parse(arg.Data.CustomId.Split('#')[5]));
                        break;
                    case "bid-amount-modal":
                        await bidRequestModalSubmitedHandler(arg, arg.Data.CustomId.Split('#')[1], ulong.Parse(arg.Data.CustomId.Split('#')[2]), ulong.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    case "communicate-modal":
                        await sendMessage(arg, arg.Data.CustomId.Split('#')[1], arg.Data.CustomId.Split('#')[2],ulong.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    case "report-problem-modal":
                        await reportProblem(arg);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in ModalSubmitted : " + ex);
            }
        }
        private async static Task _client_ButtonExecuted(SocketMessageComponent arg)
        {
            try
            {
                switch (arg.Data.CustomId.Split('#')[0])
                {
                    case "next":
                        await nextPage(arg, int.Parse(arg.Data.CustomId.Split('#')[1]), int.Parse(arg.Data.CustomId.Split('#')[2]), int.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    case "previous":
                        await previousPage(arg, int.Parse(arg.Data.CustomId.Split('#')[1]), int.Parse(arg.Data.CustomId.Split('#')[2]), int.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    case "update":
                        await updateProductModal(arg, arg.Data.CustomId.Split('#')[1],ulong.Parse(arg.Data.CustomId.Split('#')[2]), int.Parse(arg.Data.CustomId.Split('#')[3]), int.Parse(arg.Data.CustomId.Split('#')[4]), int.Parse(arg.Data.CustomId.Split('#')[5]));
                        break;
                    case "bid":
                        await bidAmount(arg, arg.Data.CustomId.Split('#')[1], ulong.Parse(arg.Data.CustomId.Split('#')[2]));
                        break;
                    case "yes":
                        await acceptBid(arg, arg.Data.CustomId.Split('#')[1], ulong.Parse(arg.Data.CustomId.Split('#')[2]), int.Parse(arg.Data.CustomId.Split('#')[3]), ulong.Parse(arg.Data.CustomId.Split('#')[4]));
                        break;
                    case "no":
                        await refuseBid(arg, arg.Data.CustomId.Split('#')[1], ulong.Parse(arg.Data.CustomId.Split('#')[2]), int.Parse(arg.Data.CustomId.Split('#')[3]), ulong.Parse(arg.Data.CustomId.Split('#')[4]));
                        break;
                    case "finish-chat":
                        await approveSaleAndBuy(arg, ulong.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    case "approve-sale":
                        await approveSale(arg, ulong.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    case "approve-buy":
                        await approveBuy(arg, ulong.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    case "send-message-to-seller":
                        await communicateModal(arg, ulong.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    case "send-message-to-buyer":
                        await communicateModal(arg, ulong.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in ButtonExecuted : " + ex);
            }
        }
        private async static Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            try
            {
                if (arg.Data.Name == "set-products-channel")
                {
                    var channel = arg.Data.Options.First().Value as IGuildChannel;
                    productsChannelId = channel.Id;
                    Console.WriteLine(productsChannelId);
                    SaveProductsChannelIdToJson(productsChannelId, "productsChannelId.json");
                    await arg.RespondAsync("You have been set the products channel !!!", ephemeral: true);
                }
                if (arg.Data.Name == "set-admins-channel")
                {
                    var channel = arg.Data.Options.First().Value as IGuildChannel;
                    adminsChannelId = channel.Id;
                    SaveAdminsChannelIdToJson(adminsChannelId, "adminsChannelId.json");
                    await arg.RespondAsync("You have been set the admins channel !!!", ephemeral: true);

                }
                if (adminsChannelId == 0 || productsChannelId == 0)
                {
                    if (adminsChannelId == 0)
                    {
                        await arg.RespondAsync("Please first set an admins channel using /set-admins-channel command !!!", ephemeral: true);

                    }
                    if (productsChannelId == 0)
                    {
                        await arg.RespondAsync("Please first set an products channel using /set-products-channel command !!!", ephemeral: true);
                    }
                    return;
                }
                switch (arg.Data.Name)
                {
                    case "create-product":
                        await createProduct(arg);
                        break;
                    case "products":
                        await seeProducts(arg);
                        break;
                    case "report-problem":
                        await reportProblemModal(arg);
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in SlashCommandExecuted : " + ex);
            }
        }
        private async static Task _client_Ready()
        {
            try
            {
                List<ChannelType> channelTypes = new List<ChannelType>() { ChannelType.Text };
                SlashCommandBuilder setProductsChannel = new SlashCommandBuilder()
                   .WithName("set-products-channel")
                   .WithDescription("Set Products Channel.")
                   .AddOption("channel", ApplicationCommandOptionType.Channel, "Choose the channel you want", channelTypes: channelTypes,isRequired:true);
                SlashCommandBuilder setAdminsChannel = new SlashCommandBuilder()
                   .WithName("set-admins-channel")
                   .WithDescription("Set Admins Channel.")
                   .AddOption("channel", ApplicationCommandOptionType.Channel, "Choose the channel you want", channelTypes: channelTypes, isRequired: true);
                SlashCommandBuilder createProduct = new SlashCommandBuilder()
                   .WithName("create-product")
                   .WithDescription("Let's create a product.")
                   .AddOption("name", ApplicationCommandOptionType.String, "Product name",isRequired:true)
                   .AddOption("image", ApplicationCommandOptionType.String, "Product image", isRequired: true)
                   .AddOption("price", ApplicationCommandOptionType.String, "Product price", isRequired: true);
                SlashCommandBuilder products = new SlashCommandBuilder()
                  .WithName("products")
                  .WithDescription("See Your Products.");
                SlashCommandBuilder reportProblem = new SlashCommandBuilder()
                 .WithName("report-problem")
                 .WithDescription("Report Problem");

                _ = await _client.CreateGlobalApplicationCommandAsync(createProduct.Build());
                _ = await _client.CreateGlobalApplicationCommandAsync(setProductsChannel.Build());
                _ = await _client.CreateGlobalApplicationCommandAsync(setAdminsChannel.Build());
                _ = await _client.CreateGlobalApplicationCommandAsync(products.Build());
                _ = await _client.CreateGlobalApplicationCommandAsync(reportProblem.Build());

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in ClientReady : " + ex);
            }
        }
        private static Task _client_Log(LogMessage arg)
        {
            Console.WriteLine($"{arg.Message}");
            return Task.CompletedTask;
        }
        #endregion

        #region Product Functions

        #region Create Product
        public async static Task createProduct(SocketSlashCommand arg)
        {
            try
            {
                Seller seller = sellers.FirstOrDefault(u => u.UserId == arg.User.Id.ToString());
                if (seller == null)
                {
                    seller = new Seller
                    {
                        UserId = arg.User.Id.ToString(),
                        UserName = arg.User.Username
                    };

                    sellers.Add(seller);
                }
                Product product = new Product();

                string productName = arg.Data.Options.First(x => x.Name == "name").Value.ToString();
                string productImage = arg.Data.Options.First(x => x.Name == "image").Value.ToString();
                int productPrice = int.Parse(arg.Data.Options.First(x => x.Name == "price").Value.ToString());

                Uri uriResult;
                bool isUrlValid = Uri.TryCreate(productImage, UriKind.Absolute, out uriResult)&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (isUrlValid)
                {
                    product.Name = productName;
                    product.Image = productImage;
                    product.MinPrice = productPrice;

                    products.Add(product);
                    seller.Products.Add(product);

                    var embed = new EmbedBuilder()
                        .WithTitle("New Product Created")
                        .WithDescription($"**Name:** {productName}\n**Price:** {productPrice}\n**Image:**")
                        .WithImageUrl(productImage)
                        .WithAuthor(_client.CurrentUser)
                        .WithCurrentTimestamp()
                        .WithColor(Color.Green)
                        .Build();
                    var bidButton = new ButtonBuilder()
                        .WithLabel("Bid")
                        .WithCustomId($"bid#{seller.UserId}#{product.Id}")
                        .WithStyle(ButtonStyle.Primary);
                    var builder = new ComponentBuilder()
                        .WithButton(bidButton).Build();
                    var channel = _client.GetChannel(productsChannelId) as IMessageChannel;
                    await channel.SendMessageAsync(embed: embed, components: builder);
                    Seller.SaveAllToJson(sellers, "sellers.json");
                    await arg.RespondAsync("Your product has been send to the channel", ephemeral: true);
                }
                else
                {
                    await arg.RespondAsync("ProductImage must be  url format.", ephemeral: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in createProduct: " + ex);
            }
        }
        #endregion

        #region Product Management
        public static async Task seeProducts(SocketSlashCommand arg)
        {

            Seller seller = sellers.Find(c => c.UserId == arg.User.Id.ToString());

            const int ProductsPerPage = 1;
            int currentPageIndex = 0;
            int totalPageNumber = 0;
            int totalProductNumber = 0;

            foreach (Product product in seller.Products)
            {
                totalProductNumber++;
            }
            totalPageNumber = (totalProductNumber / ProductsPerPage) + ((totalProductNumber % ProductsPerPage == 0) ? 0 : 1);

            var productsEmbed = new EmbedBuilder()
                .WithTitle($"Your Products")
                .WithAuthor(_client.CurrentUser)
                .WithColor(Color.Magenta)
                .WithCurrentTimestamp();
            var previousButton = new ButtonBuilder()
                 .WithLabel("⬅️ Previous Page")
                 .WithCustomId($"previous#{currentPageIndex}#{totalProductNumber}#{ProductsPerPage} #false")
                 .WithStyle(ButtonStyle.Danger);
            var currentPageButton = new ButtonBuilder()
                .WithLabel($"Current Page {currentPageIndex + 1}/{totalPageNumber}")
                .WithCustomId("current#")
                .WithStyle(ButtonStyle.Primary)
                .WithDisabled(true);
            var nextPageButton = new ButtonBuilder()
                .WithLabel(" Next Page ➡️")
                .WithCustomId($"next#{currentPageIndex}#{totalProductNumber}#{ProductsPerPage} #false")
                .WithStyle(ButtonStyle.Success);
            var updateProductButton = new ButtonBuilder()
                .WithLabel("Update")
                .WithStyle(ButtonStyle.Primary);

            int startIndex = currentPageIndex * ProductsPerPage;
            int endIndex = startIndex + ProductsPerPage;

            for (int j = startIndex; j < endIndex && j < seller.Products.Count; j++)
            {
                Product product = seller.Products[j];

                productsEmbed.Description += ($"{j + 1} )\n Product Name : {product.Name}\n Product MinPrice : {product.MinPrice}");
                updateProductButton.WithCustomId($"update#{seller.UserId}#{product.Id}#{currentPageIndex}#{totalProductNumber}#{ProductsPerPage}");
                productsEmbed.ImageUrl+=(product.Image);
            }

            if (currentPageIndex == 0)
            {
                previousButton.WithDisabled(true);
            }
            if ((currentPageIndex + 1) == (totalPageNumber))
            {
                nextPageButton.WithDisabled(true);
            }
            var builder = new ComponentBuilder().WithButton(previousButton).WithButton(currentPageButton).WithButton(nextPageButton).WithButton(updateProductButton, row: 2).Build();
            await arg.RespondAsync(components: builder, embed: productsEmbed.Build(), ephemeral: true);

        }
        public static async Task nextPage(SocketMessageComponent arg, int currentPage, int totalProductNumber, int ProductsPerPage)
        {
            currentPage++;

            await modifySeeProducts(arg, currentPage, totalProductNumber, ProductsPerPage);


        }
        public static async Task previousPage(SocketMessageComponent arg, int currentPage, int totalProductNumber, int ProductsPerPage)
        {
            currentPage--;

            await modifySeeProducts(arg, currentPage, totalProductNumber, ProductsPerPage);

        }
        public static async Task modifySeeProducts(IDiscordInteraction arg, int currentPageIndex, int totalProductNumber, int ProductsPerPage)
        {
            Seller seller = sellers.Find(c => c.UserId == arg.User.Id.ToString());
            await arg.DeferAsync();
            int totalPageNumber = totalProductNumber / ProductsPerPage;

            var productsEmbed = new EmbedBuilder()
                .WithTitle($"Your Products")
                .WithAuthor(_client.CurrentUser)
                .WithColor(Color.Magenta)
                .WithCurrentTimestamp();
            var previousButton = new ButtonBuilder()
                 .WithLabel("⬅️ Previous Page")
                 .WithCustomId($"previous#{currentPageIndex}#{totalProductNumber}#{ProductsPerPage} #false")
                 .WithStyle(ButtonStyle.Danger);
            var currentPageButton = new ButtonBuilder()
                .WithLabel($"Current Page {currentPageIndex + 1}/{totalPageNumber}")
                .WithCustomId("current#")
                .WithStyle(ButtonStyle.Primary)
                .WithDisabled(true);
            var nextPageButton = new ButtonBuilder()
                .WithLabel(" Next Page ➡️")
                .WithCustomId($"next#{currentPageIndex}#{totalProductNumber}#{ProductsPerPage} #false")
                .WithStyle(ButtonStyle.Success);
            var updateProductButton = new ButtonBuilder()
                .WithLabel("Update")
                .WithStyle(ButtonStyle.Primary);

            int startIndex = currentPageIndex * ProductsPerPage;
            int endIndex = startIndex + ProductsPerPage;

            for (int j = startIndex; j < endIndex && j < seller.Products.Count; j++)
            {
                Product product = seller.Products[j];

                productsEmbed.Description += ($"{j + 1} )\n Product Name : {product.Name}\n Product MinPrice : {product.MinPrice}");
                updateProductButton.WithCustomId($"update#{seller.UserId}#{product.Id}#{currentPageIndex}#{totalProductNumber}#{ProductsPerPage}");
                productsEmbed.ImageUrl += (product.Image);
            }

            if (currentPageIndex == 0)
            {
                previousButton.WithDisabled(true);
            }
            if ((currentPageIndex + 1) == (totalPageNumber))
            {
                nextPageButton.WithDisabled(true);
            }
            var builder = new ComponentBuilder().WithButton(previousButton).WithButton(currentPageButton).WithButton(nextPageButton).WithButton(updateProductButton, row: 2).Build();

            if (arg is SocketModal)
            {
                await (arg as SocketModal).ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = productsEmbed.Build();
                    res.Components = builder;
                });
            }
            if (arg is SocketMessageComponent)
            {
                await (arg as SocketMessageComponent).ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = productsEmbed.Build();
                    res.Components = builder;
                });
            }
        }
        public static async Task updateProductModal(SocketMessageComponent arg, string sellerId, ulong productId, int currentPageIndex, int totalProductNumber, int ProductsPerPage)
        {
            ModalBuilder updateProductModal = new ModalBuilder()
                         .WithTitle("Input updates you want")
                         .WithCustomId($"update#{sellerId}#{productId}#{currentPageIndex}#{totalProductNumber}#{ProductsPerPage}")
                         .AddTextInput("Name", "name", placeholder: "Input a name")
                         .AddTextInput("ImageURL", "imgurl", placeholder: "Input an imageurl")
                         .AddTextInput("Min Price", "minprice", placeholder: "Input a minimum price");
            await arg.RespondWithModalAsync(modal: updateProductModal.Build());
        }
        public static async Task updateProduct(SocketModal arg, string sellerId, ulong productId, int currentPageIndex, int totalProductNumber, int ProductsPerPage)
        {
            Seller seller = sellers.Find(c => c.UserId == sellerId);
            Product product = seller.Products.Find(c => c.Id == productId);

            string name = arg.Data.Components.FirstOrDefault(x => x.CustomId == "name")?.Value;
            string imageurl = arg.Data.Components.FirstOrDefault(x => x.CustomId == "imgurl")?.Value;
            string minPriceString = arg.Data.Components.FirstOrDefault(x => x.CustomId == "minprice")?.Value;

            Uri uriResult;
            bool isUrlValid = Uri.TryCreate(imageurl, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (isUrlValid)
            {
                if (name != null)
                {
                    product.Name = name;
                }
                if (imageurl != null)
                {
                    product.Image = imageurl;
                }
                if (minPriceString != null && int.TryParse(minPriceString, out int minPrice))
                {
                    product.MinPrice = minPrice;
                }

                Seller.SaveAllToJson(sellers, "sellers.json");

                await modifySeeProducts(arg, currentPageIndex, totalProductNumber, ProductsPerPage);
            }
            else
            {
                await arg.RespondAsync("ProductImage must be  url format.", ephemeral: true);
            }
        }
        #endregion

        #region Bid
        public async static Task bidRequestModalSubmitedHandler(SocketModal arg, string sellerId, ulong productId,ulong buyerId)
        {
            string bidAmountString = arg.Data.Components.FirstOrDefault(x => x.CustomId == "bid-amount")?.Value;
            if (bidAmountString != null && int.TryParse(bidAmountString, out int bidAmount))
            {
                await bidRequest(arg,sellerId, productId,bidAmount,buyerId);
            }
        }
        public async static Task bidRequest(SocketModal arg, string sellerId, ulong productId,int bidAmount,ulong buyerId)
        {
            Seller seller = sellers.Find(c => c.UserId == sellerId);
            Product product = seller.Products.Find(c=>c.Id== productId);
            string messageContent = $"Do you accept offer **{bidAmount}** for product **{product.Name}** ?";

            var embed =new EmbedBuilder()
                    .WithTitle("New Bid")
                    .WithDescription(messageContent)
                    .WithAuthor(_client.CurrentUser)
                    .WithCurrentTimestamp()
                    .WithColor(Color.Green)
                    .Build();
            var yesButton = new ButtonBuilder()
                .WithLabel("Yes")
                .WithCustomId($"yes#{sellerId}#{productId}#{bidAmount}#{buyerId}")
                .WithStyle(ButtonStyle.Success);
            var noButton = new ButtonBuilder()
                .WithLabel("No")
                .WithCustomId($"no#{sellerId}#{productId}#{bidAmount}#{buyerId}")
                .WithStyle(ButtonStyle.Danger);
            var builder = new ComponentBuilder()
                .WithButton(yesButton).WithButton(noButton).Build();

            IGuild guild = _client.GetGuild(guildId);

            ulong sellerIdUlong;
            if (ulong.TryParse(sellerId, out sellerIdUlong))
            {
                // Kullanıcıya özel mesaj gönder
                var sellerUser = await guild.GetUserAsync(sellerIdUlong);

                Console.WriteLine(sellerUser);
                if (sellerUser != null)
                {
                    await sellerUser.SendMessageAsync(embed:embed,components:builder);
                    await arg.RespondAsync("Your offer has been sended to seller",ephemeral:true);
                }
                else
                {
                    await arg.RespondAsync("Unfortunatelly ,This Seller Not Founded Now", ephemeral: true);
                }
            }
            else
            {
                Console.WriteLine("allfather");
            }
        }
        public async static Task bidAmount(SocketMessageComponent arg, string sellerId, ulong productId)
        {
            ModalBuilder bidAmountModal = new ModalBuilder()
                        .WithTitle("Input bid amount")
                        .WithCustomId($"bid-amount-modal#{sellerId}#{productId}#{arg.User.Id}")//split[3] = buyerId
                        .AddTextInput("Bid amount", "bid-amount", placeholder: "Input a bid amount");
            await arg.RespondWithModalAsync(modal: bidAmountModal.Build());
        }
        public async static Task acceptBid(SocketMessageComponent arg, string sellerId, ulong productId, int bidAmount, ulong buyerId)
        {
            Seller seller = sellers.Find(c => c.UserId == sellerId);
            Product product = seller.Products.Find(c => c.Id == productId);
            Buyer buyer = new Buyer(buyerId);

            Anonim anonim = anonims.FirstOrDefault(c => c.Seller.UserId == sellerId && c.Buyer.UserId == buyerId && c.Product.Id==productId);
            if (anonim == null)
            {
                anonim = new Anonim(seller, buyer, product);
                anonims.Add(anonim);
            }

            Console.WriteLine("bidder : " + buyerId);
            IGuild guild = _client.GetGuild(guildId);

            var buyerUser = await guild.GetUserAsync(buyerId);

            if (buyerUser != null)
            {
                var buyerEmbed = new EmbedBuilder()
                    .WithTitle("Chat With Seller")
                    .WithDescription($"You can talk about **Name:** {anonim.Product.Name} with seller to use **Send Message To Seller** button\n")
                    .WithAuthor(_client.CurrentUser)
                    .WithCurrentTimestamp()
                    .WithColor(Color.Green)
                    .Build();
                var buyerButton = new ButtonBuilder()
                    .WithLabel("Send Message To Seller")
                    .WithCustomId($"send-message-to-seller#{anonim.Seller.UserId}#{anonim.Buyer.UserId}#{anonim.Product.Id}")
                    .WithStyle(ButtonStyle.Primary);
                var buyerBuilder = new ComponentBuilder().WithButton(buyerButton).Build();
                var sellerEmbed = new EmbedBuilder()
                    .WithTitle("Chat With Buyer")
                    .WithDescription($"You can talk about **Name:** {anonim.Product.Name}  with buyer to use **Send Message To Buyer** button\nIf you click **Finish Chat** bot sends approve request both of you")
                    .WithAuthor(_client.CurrentUser)
                    .WithCurrentTimestamp()
                    .WithColor(Color.Green)
                    .Build();
                var sellerButton = new ButtonBuilder()
                    .WithLabel("Send Message To Buyer")
                    .WithCustomId($"send-message-to-buyer#{anonim.Seller.UserId}#{anonim.Buyer.UserId}#{anonim.Product.Id}")
                    .WithStyle(ButtonStyle.Primary);
                var ApproveButton = new ButtonBuilder()
                   .WithLabel("Finish Chat")
                   .WithCustomId($"finish-chat#{anonim.Seller.UserId}#{anonim.Buyer.UserId}#{anonim.Product.Id}")
                   .WithStyle(ButtonStyle.Danger);
                var sellerBuilder = new ComponentBuilder().WithButton(sellerButton).WithButton(ApproveButton).Build();
                await buyerUser.SendMessageAsync(embed:buyerEmbed,components:buyerBuilder);
                await arg.RespondAsync("Your accept message has been sent to the bidder.", embed: sellerEmbed, components: sellerBuilder, ephemeral: true);
            }
            else
            {
                await arg.RespondAsync("Unfortunately, this buyer was not found in the guild.");
            }

            Anonim.SaveAllToJson(anonims, "anonims.json");
        }
        public async static Task refuseBid(SocketMessageComponent arg,string sellerId, ulong productId, int bidAmount,ulong buyerId)
        {
            Seller seller = sellers.Find(c=>c.UserId==sellerId);
            Product product = seller.Products.Find(c=>c.Id==productId);

            // Kullanıcıya özel mesajı gönder
            IGuild guild = _client.GetGuild(guildId);

            var buyerUser = await guild.GetUserAsync(buyerId); 
            if (buyerUser != null)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Your bid has been refused")
                    .WithDescription($"Your offer **{bidAmount}** for product **{product.Name}** has been refused.")
                    .WithAuthor(_client.CurrentUser)
                    .WithCurrentTimestamp()
                    .WithColor(Color.Green)
                    .Build();

                await buyerUser.SendMessageAsync(embed:embed);
                await arg.RespondAsync("Your refuse message has been sended to the bidder.", ephemeral: true);
            }
            else
            {
                await arg.RespondAsync("Unfortunatelly ,This Buyer Not Founded Now",ephemeral:true);
            }


        }
        #endregion

        #region Anonim Communication
        public async static Task communicateModal(SocketMessageComponent arg,ulong productId)
        {
            var modalCustomId= "";
            Anonim anonim = anonims.Find(c => (c.Seller.UserId == arg.User.Id.ToString() || c.Buyer.UserId == arg.User.Id) && c.Product.Id ==productId);
            if (arg.User.Id.ToString() == anonim.Seller.UserId) 
            {
                modalCustomId = $"communicate-modal#toBuyer#{anonim.Buyer.UserId}#{productId}";
            }
            else if (arg.User.Id==anonim.Buyer.UserId)
            {
                modalCustomId = $"communicate-modal#toSeller#{anonim.Seller.UserId}#{productId}";
            }
            else
            {
                await arg.RespondAsync("You have not any communication");
            }

            ModalBuilder communicateModal = new ModalBuilder()
                       .WithTitle("Input Message")
                       .WithCustomId(modalCustomId)
                       .AddTextInput("Message" , "message", placeholder: "Message");
            await arg.RespondWithModalAsync(modal: communicateModal.Build());
        }
        public async static Task sendMessage(SocketModal arg, string toWho, string recieverId,ulong productId)
        {
            string message = arg.Data.Components.FirstOrDefault(x => x.CustomId == "message")?.Value;

            Anonim anonim = anonims.Find(c => (c.Seller.UserId == arg.User.Id.ToString() || c.Buyer.UserId == arg.User.Id)&&c.Product.Id==productId);

            if (toWho == "toBuyer")
            {
                IGuild guild = _client.GetGuild(guildId);

                // Convert the receiverId to ulong
                ulong receiverIdUlong;
                if (ulong.TryParse(recieverId, out receiverIdUlong))
                {
                    // Retrieve the user from the guild
                    var buyerUser = await guild.GetUserAsync(receiverIdUlong);

                    if (buyerUser != null)
                    {
                        var embed=new EmbedBuilder()
                            .WithTitle($"New Message From Seller For {anonim.Product.Name}")
                            .WithDescription(message)
                            .WithAuthor(_client.CurrentUser)
                            .WithColor(Color.Magenta)
                            .WithFooter("You can answer this message use /communicate-with-seller command")
                            .WithCurrentTimestamp()
                            .Build();
                        await buyerUser.SendMessageAsync(embed:embed);

                        await arg.RespondAsync("Your Message has been sended to the buyer",ephemeral:true);
                    }
                    else
                    {
                        await arg.RespondAsync("Problem");
                    }
                }
                else
                {
                    await arg.RespondAsync("Problem");
                }
            }
            else if (toWho == "toSeller")
            {
                IGuild guild = _client.GetGuild(guildId);

                ulong receiverIdUlong;
                if (ulong.TryParse(recieverId, out receiverIdUlong))
                {
                    // Retrieve the user from the guild
                    var sellerUser = await guild.GetUserAsync(receiverIdUlong);

                    if (sellerUser != null)
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle($"New Message From Buyer For {anonim.Product.Name}")
                            .WithDescription(message)
                            .WithAuthor(_client.CurrentUser)
                            .WithColor(Color.Magenta)
                            .WithFooter("You can answer this message use /communicate-with-buyer command")
                            .WithCurrentTimestamp()
                            .Build();
                        await sellerUser.SendMessageAsync(embed: embed);

                        await arg.RespondAsync("Your Message has been sended to the seller",ephemeral:true);
                    }
                    else
                    {
                        // Handle the case where the sellerUser is not found
                        await arg.RespondAsync("Problem");

                    }
                }
                else
                {
                    // Handle the case where recieverId cannot be parsed to ulong
                    await arg.RespondAsync("Problem");
                }
            }
        }
        #endregion

        #region Sale Transaction and Approvals
        public async static Task approveSaleAndBuy(SocketMessageComponent arg,ulong productId)
        {
            Anonim anonim = anonims.Find(c => (c.Seller.UserId == arg.User.Id.ToString() || c.Buyer.UserId == arg.User.Id)&&c.Product.Id==productId);

            if (anonim != null)
            {
                IGuild guild = _client.GetGuild(guildId);

                // Check if the command user is the seller

                var sellerUser = await guild.GetUserAsync(ulong.Parse(anonim.Seller.UserId));
                if (sellerUser != null)
                {
                    var embed = new EmbedBuilder()
                        .WithTitle($"Approve Sale")
                        .WithDescription("Do you confirm that you received the money?")
                        .WithAuthor(_client.CurrentUser)
                        .WithColor(Color.Magenta)
                        .WithFooter("You can report any issue to our admins use /report-issue command")
                        .WithCurrentTimestamp()
                        .Build();
                    var button = new ButtonBuilder()
                        .WithLabel("Approve")
                        .WithCustomId($"approve-sale#{anonim.Seller.UserId}#{anonim.Buyer.UserId}#{productId}")
                        .WithStyle(ButtonStyle.Primary);
                    var builder = new ComponentBuilder().WithButton(button).Build();
                    await sellerUser.SendMessageAsync(embed: embed, components: builder);
                }
                else
                {
                    //await arg.User.SendMessageAsync("Sale approved. Message for seller (A)");
                }

                // Check if the command user is the buyer

                var buyerUser = await guild.GetUserAsync(anonim.Buyer.UserId);
                if (buyerUser != null)
                {
                    var embed = new EmbedBuilder()
                        .WithTitle($"Approve Buy")
                        .WithDescription("Do you confirm that you buy the product?")
                        .WithAuthor(_client.CurrentUser)
                        .WithColor(Color.Magenta)
                        .WithFooter("You can report any issue to our admins use /report-issue command")
                        .WithCurrentTimestamp()
                        .Build();
                    var button = new ButtonBuilder()
                        .WithLabel("Approve")
                        .WithCustomId($"approve-buy#{anonim.Seller.UserId}#{anonim.Buyer.UserId}#{productId}")
                        .WithStyle(ButtonStyle.Primary);
                    var builder = new ComponentBuilder().WithButton(button).Build();
                    await buyerUser.SendMessageAsync(embed: embed, components: builder);
                }
                else
                {
                    //await arg.User.SendMessageAsync("Sale approved. Message for buyer (B)");
                }
                await arg.RespondAsync("Your approve request has been sended to seller and you", ephemeral: true);

            }
        }
        public async static Task approveSale(SocketMessageComponent arg,ulong productId)
        {
            Anonim anonim = anonims.Find(c => c.Seller.UserId == arg.User.Id.ToString() && c.Product.Id ==productId);
            if (anonim!=null)
            {
                anonim.SellerAprove = true;
                await arg.RespondAsync("You have been aproved the sale");
                if (anonim.SellerAprove && anonim.BuyerAprove)
                {
                    await finishSale(arg, anonim);
                }
            }
        }
        public async static Task approveBuy(SocketMessageComponent arg,ulong productId)
        {
            Anonim anonim = anonims.Find(c =>c.Buyer.UserId == arg.User.Id && c.Product.Id==productId);
            if (anonim != null)
            {
                anonim.BuyerAprove = true;
                await arg.RespondAsync("You have been aproved the buy");
                if (anonim.SellerAprove && anonim.BuyerAprove)
                {
                    await finishSale(arg, anonim);
                }

            }
        }
        public async static Task finishSale(SocketMessageComponent arg,Anonim anonim)
        {
            Seller seller = sellers.Find(c=>c.UserId==anonim.Seller.UserId);
            Product product = seller.Products.Find(c => c.Id == anonim.Product.Id);
            IGuild guild = _client.GetGuild(guildId);

            // Check if the command user is the seller
            var sellerUser = await guild.GetUserAsync(ulong.Parse(anonim.Seller.UserId));
            if (sellerUser != null)
            {
                var embed = new EmbedBuilder()
                    .WithTitle($"Your sale is finished")
                    .WithDescription($" Product : {anonim.Product.Name}")
                    .WithAuthor(_client.CurrentUser)
                    .WithColor(Color.Magenta)
                    .WithFooter("Thank you for choose us")
                    .WithCurrentTimestamp()
                    .Build();
                await sellerUser.SendMessageAsync(embed: embed);
            }
            else
            {
                //await arg.User.SendMessageAsync("Sale approved. Message for seller (A)");
            }

            // Check if the command user is the buyer

            var buyerUser = await guild.GetUserAsync(anonim.Buyer.UserId);
            if (buyerUser != null)
            {
                var embed = new EmbedBuilder()
                    .WithTitle($"Your buy is finished")
                    .WithDescription($" Product : {anonim.Product.Name}")
                    .WithAuthor(_client.CurrentUser)
                    .WithColor(Color.Magenta)
                    .WithFooter("Thank you for choose us")
                    .WithCurrentTimestamp()
                    .Build();
                await buyerUser.SendMessageAsync(embed: embed);
            }
            else
            {
                //await arg.User.SendMessageAsync("Sale approved. Message for buyer (B)");
            }

            product.isSaled = true;
            anonims.Remove(anonim);
            Seller.SaveAllToJson(sellers, "sellers.json");
            Anonim.SaveAllToJson(anonims, "anonims.json");
            
        }
        public async static Task reportProblemModal(SocketSlashCommand arg) 
        {
            ModalBuilder reportProblemModal = new ModalBuilder()
                       .WithTitle("Input Message")
                       .WithCustomId("report-problem-modal")
                       .AddTextInput("Message", "message", placeholder: "Message");
            await arg.RespondWithModalAsync(modal: reportProblemModal.Build());
        }
        public async static Task reportProblem(SocketModal arg)
        {

            string message = arg.Data.Components.FirstOrDefault(x => x.CustomId == "message")?.Value;

            var channel = _client.GetChannel(adminsChannelId) as IMessageChannel;
            var embed = new EmbedBuilder()
                   .WithTitle("New Problem")
                   .WithDescription($"From <@{arg.User.Id}>\n{message}")
                   .WithAuthor(_client.CurrentUser)
                   .WithCurrentTimestamp()
                   .WithColor(Color.Green)
                   .Build();
            await channel.SendMessageAsync(embed: embed);

            await arg.RespondAsync("Thank you for report the problem.We will help you asap",ephemeral:true);

        }

        #endregion

        #endregion

        #region Save and Load
        private static List<Seller> LoadSellersFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {

                string json = File.ReadAllText(filePath);
                return json.Length != 0 ? JsonConvert.DeserializeObject<List<Seller>>(json) : new List<Seller>();
            }
            else
            {
                Console.WriteLine("dosya bulunamadı");
            }
            return new List<Seller>();
        }
        private static List<Anonim> LoadAnonimsFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {

                string json = File.ReadAllText(filePath);
                return json.Length != 0 ? JsonConvert.DeserializeObject<List<Anonim>>(json) : new List<Anonim>();
            }
            else
            {
                Console.WriteLine("dosya bulunamadı");
            }
            return new List<Anonim>();
        }
        public static void SaveProductsChannelIdToJson(ulong channelId, string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(channelId, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"duartion saved to {filePath} successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving duration to {filePath}: {ex.Message}");
            }
        }
        private static ulong LoadPrdouctsChannelIdFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return !string.IsNullOrEmpty(json) ? JsonConvert.DeserializeObject<ulong>(json) : 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Dosya okuma hatası: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Dosya bulunamadı");
            }

            return 0;
        }
        public static void SaveAdminsChannelIdToJson(ulong channelId, string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(channelId, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"duartion saved to {filePath} successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving duration to {filePath}: {ex.Message}");
            }
        }
        private static ulong LoadAdminsChannelIdFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return !string.IsNullOrEmpty(json) ? JsonConvert.DeserializeObject<ulong>(json) : 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Dosya okuma hatası: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Dosya bulunamadı");
            }

            return 0;
        }
        #endregion
    }
}
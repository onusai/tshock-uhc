using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using System.Text.Json;
using TShockAPI.Hooks;

namespace UHC
{
    [ApiVersion(2, 1)]
    public class UHC : TerrariaPlugin
    {

        public override string Author => "Onusai";
        public override string Description => "HP does not regenerate";
        public override string Name => "UHC";
        public override Version Version => new Version(1, 0, 0, 0);

        public class ConfigData
        {
            public bool Enabled { get; set; } = true;

            public Dictionary<int, int> HealingItems { get; set; } = new Dictionary<int, int>()
            {
                { 29, 50 },
                { 3335, 600 }
            };
            public Dictionary<string, int> PlayerHP { get; set; } = new Dictionary<string, int>();
        }

        ConfigData config;

        Dictionary<string, int> PlayerForceHP { get; set; } = new Dictionary<string, int>();

        public UHC(Main game) : base(game) { }

        public override void Initialize()
        {
            config = PluginConfig.Load("UHC");
            ServerApi.Hooks.GameInitialize.Register(this, OnGameLoad);
        }

        void OnGameLoad(EventArgs e)
        {
            if (!config.Enabled) return;

            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += OnPlayerPostLogin;
            ServerApi.Hooks.WorldSave.Register(this, OnWorldSave);
            GetDataHandlers.PlayerDamage += OnPlayerDamage;
            GetDataHandlers.PlayerHP += OnPlayerHP;
            GetDataHandlers.PlayerUpdate += OnPlayerUpdate;

            RegisterCommand("sethp", "tshock.admin", CMDOnSetHP, "Used to set UHC player hp\nUsage: /sethp <amount> <player name>\nExample: /sethp 400 onusai");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameLoad);

                if (config.Enabled)
                {
                    TShockAPI.Hooks.PlayerHooks.PlayerPostLogin -= OnPlayerPostLogin;
                    ServerApi.Hooks.WorldSave.Deregister(this, OnWorldSave);
                    GetDataHandlers.PlayerDamage -= OnPlayerDamage;
                    GetDataHandlers.PlayerHP -= OnPlayerHP;
                    GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
                }
            }
            base.Dispose(disposing);
        }

        void RegisterCommand(string name, string perm, CommandDelegate handler, string helptext)
        {
            TShockAPI.Commands.ChatCommands.Add(new Command(perm, handler, name) { HelpText = helptext });
        }

        void OnPlayerPostLogin(PlayerPostLoginEventArgs e)
        {
            TSPlayer player = e.Player;
            if (player == null) return;

            int hp = 0;

            if (config.PlayerHP.ContainsKey(player.Name))
                hp = config.PlayerHP[player.Name];
            else
                hp = player.TPlayer.statLife;

            if (hp <= 0)
            {

                player.Kick("You are dead", true, true);
                return;
            }

            SetHP(hp, player);
            player.SetData<bool>("rdy", true);
        }

        void OnPlayerHP(Object sender, GetDataHandlers.PlayerHPEventArgs e)
        {
            if (!e.Player.GetData<bool>("rdy")) return;

            string name = e.Player.Name;
            int uhp = config.PlayerHP[name];

            if (e.Current > uhp)
                SetHP(uhp, e.Player);
            else
                config.PlayerHP[name] = Math.Clamp(e.Current, 0, e.Player.TPlayer.statLifeMax);
            
        }

        void OnPlayerDamage(Object sender, GetDataHandlers.PlayerDamageEventArgs e)
        {
            config.PlayerHP[e.Player.Name] -= Math.Clamp(e.Damage, 0, e.Player.TPlayer.statLifeMax);
        }


        void SetHP(int hp, TSPlayer player)
        {
            hp = Math.Clamp(hp, 0, player.TPlayer.statLifeMax);
            config.PlayerHP[player.Name] = hp;
            player.TPlayer.statLife = hp;
            NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, null, player.TPlayer.whoAmI);
        }

        void CMDOnSetHP(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage("Usage: /sethp <amount> <player name>\nExample: /sethp 400 onusai\nExample: /sethp 300 \"corn stick\"");
                return;
            }

            int amount = 0;
            if (!int.TryParse(args.Parameters[0], out amount))
            {
                args.Player.SendErrorMessage(String.Format("Invalid HP amount: {0}", args.Parameters[0]));
                return;
            }

            if (config.PlayerHP.ContainsKey(args.Parameters[1]))
            {
                // in case you want to revive dead player
                config.PlayerHP[args.Parameters[1]] = amount;
            }

            List<TSPlayer> players = TSPlayer.FindByNameOrID(args.Parameters[1]);
            if (players.Count == 0)
            {
                args.Player.SendErrorMessage(String.Format("Unable to find player with username: {0}. Try surrounding the name in \"quotes\"", args.Parameters[1]));
                return;
            }

            TSPlayer player = players[0];
            SetHP(amount, player);
        }

        void OnWorldSave(WorldSaveEventArgs e)
        {
            PluginConfig.Save(config);
        }

        void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs e)
        {
            if (e.Control.IsUsingItem && !e.Player.TPlayer.controlUseItem)
            {
                Item item = e.Player.TPlayer.inventory[e.SelectedItem];

                if (!config.HealingItems.ContainsKey(item.type)) return;

                if (config.PlayerHP[e.Player.Name] == e.Player.TPlayer.statLifeMax) return;

                int healAmount = config.HealingItems[item.type];

                if (item.stack == 1)
                    item = new Item();
                else
                    item.stack -= 1;

                e.Player.TPlayer.inventory[e.SelectedItem] = item;
                NetMessage.SendData((int)PacketTypes.PlayerSlot, e.PlayerId, -1, null, e.PlayerId, e.SelectedItem);

                SetHP(config.PlayerHP[e.Player.Name] + healAmount, e.Player);
            }
        }

        public static class PluginConfig
        {
            public static string filePath;
            public static ConfigData Load(string Name)
            {
                filePath = String.Format("{0}/{1}.json", TShock.SavePath, Name);

                if (!File.Exists(filePath))
                {
                    var data = new ConfigData();
                    Save(data);
                    return data;
                }

                var jsonString = File.ReadAllText(filePath);
                var myObject = JsonSerializer.Deserialize<ConfigData>(jsonString);

                return myObject;
            }

            public static void Save(ConfigData myObject)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize(myObject, options);

                File.WriteAllText(filePath, jsonString);
            }
        }
    }
}
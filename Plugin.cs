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

            //ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
            //ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += OnPlayerPostLogin;
            ServerApi.Hooks.WorldSave.Register(this, OnWorldSave);
            GetDataHandlers.PlayerDamage += OnPlayerDamage;
            GetDataHandlers.PlayerHP += OnPlayerHP;
            //RegisterCommand("ho", "", HealO, "");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameLoad);

                if (config.Enabled)
                {
                    //ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
                    //ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
                    TShockAPI.Hooks.PlayerHooks.PlayerPostLogin -= OnPlayerPostLogin;
                    ServerApi.Hooks.WorldSave.Deregister(this, OnWorldSave);
                    GetDataHandlers.PlayerDamage -= OnPlayerDamage;
                    GetDataHandlers.PlayerHP -= OnPlayerHP;
                }
            }
            base.Dispose(disposing);
        }

        void RegisterCommand(string name, string perm, CommandDelegate handler, string helptext)
        {
            TShockAPI.Commands.ChatCommands.Add(new Command(perm, handler, name) { HelpText = helptext });
        }

        void Print(string text)
        {
            TShock.Utils.Broadcast(text, Color.White);
        }

        void OnPlayerHP(Object sender, GetDataHandlers.PlayerHPEventArgs e)
        {

            if (!e.Player.GetData<bool>("rdy")) return;
            /*
            
            if (PlayerForceHP.ContainsKey(name))
            {
                Print("forced hp");
                SetHP(PlayerForceHP[name], e.Player);
                PlayerForceHP.Remove(name);
                e.Current = (short)PlayerForceHP[name];
                return;
            }
                        */
            string name = e.Player.Name;
            int uhp = config.PlayerHP[name];

            //if (e.Current != uhp)
            //{
            //    SetHP(uhp, e.Player);
            //}

            
            if (e.Current > uhp)
            {
                if (uhp <= 0)
                {
                    uhp = e.Player.TPlayer.statLifeMax;
                    config.PlayerHP[name] = uhp;
                }
                SetHP(uhp, e.Player);
                //Print(String.Format("khp: {0} -> {1}", e.Current, uhp));
                
            }
            else
            {
                //Print(String.Format("hp: {0}", e.Current));
                config.PlayerHP[name] = e.Current;
                //SetHP(e.Current, e.Player);
            }
            

           // e.Handled = true;

            //Print(String.Format("hp - {0}", e.Current));

        }

        void OnPlayerDamage(Object sender, GetDataHandlers.PlayerDamageEventArgs e)
        {
            config.PlayerHP[e.Player.Name] -= e.Damage;
        }

        void OnPlayerPostLogin(PlayerPostLoginEventArgs e)
        {
            TSPlayer player = e.Player;// TShock.Players[e.Who];
            if (player == null) return;

            int hp = 0;

            if (config.PlayerHP.ContainsKey(player.Name))
                hp = config.PlayerHP[player.Name];
            else
                hp = player.TPlayer.statLife;

            SetHP(hp, player);


            player.SetData<bool>("rdy", true);

            //Print(String.Format("joined {0} hp: {1}", player.Name, hp));

        }
        void OnServerJoin(JoinEventArgs e)
        {

        }

        void OnServerLeave(LeaveEventArgs e)
        {

        }


        void OnWorldSave(WorldSaveEventArgs e)
        {
            PluginConfig.Save(config);
        }

        void SetHP(int hp, TSPlayer player)
        {
            config.PlayerHP[player.Name] = hp;
            player.TPlayer.statLife = hp;
            NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, null, player.TPlayer.whoAmI);
            //Print(String.Format("sethp: {0}", hp));
        }

        void HealO(CommandArgs e)
        {
            SetHP(e.TPlayer.statLifeMax, e.Player);
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace StardewMod
{
    public class ModEntry : Mod
    {
        private CustomMenu customMenu;
        private bool customTabCreated = false;
        private int interval = 20;
        private int remainingTime;
        private DateTime lastAlertTime;
        private Texture2D mushroomStandingTexture;
        private Texture2D mushroomWalkingTexture;
        public DateTime MenuOpenedTime { get; set; }

        private int numberOfWalkingMushrooms = 2;

        public List<Vector2> MushroomPositions { get; private set; }

        private BirthdayReminder birthdayReminder;

        public override void Entry(IModHelper helper)
        {
            mushroomStandingTexture = Helper.ModContent.Load<Texture2D>("assets/mushroomStandingTexture.png");
            mushroomWalkingTexture = helper.ModContent.Load<Texture2D>("assets/mushroomWalkingTexture.png");

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            //helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            //this.birthdayReminder = new BirthdayReminder(helper, this.Monitor);

            /*  if (this.birthdayReminder == null)
              {
                  this.Monitor.Log("birthdayReminder is null", LogLevel.Error);
              }
              else
              {
                  var nextBirthdayNPCAndGifts = this.birthdayReminder.GetNextBirthdayNPCAndGifts();
              } */


            //var nextBirthdayNPCAndGifts = this.birthdayReminder.GetNextBirthdayNPCAndGifts();



            this.MushroomPositions = new List<Vector2>();
            for (int i = 0; i < numberOfWalkingMushrooms; i++)
            {
                this.MushroomPositions.Add(new Vector2(i * 32, Game1.viewport.Height - mushroomWalkingTexture.Height));
            }


            //new CustomMenu(menuX, menuY, menuWidth, menuHeight, this, mushroomStandingTexture, mushroomWalkingTexture, this.MushroomPositions, Game1.graphics.GraphicsDevice, Monitor, birthdayReminder, nextBirthdayNPCAndGifts);
        }






        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            remainingTime = interval * 60;
        }

        public class BirthdayReminder
        {
            private static NPC birthdayNPC;
            private IModHelper helper;
            private IMonitor monitor;

            public BirthdayReminder(IModHelper helper, IMonitor monitor)
            {
                this.helper = helper;
                this.monitor = monitor;
                this.helper.Events.GameLoop.DayEnding += this.OnDayEnding;
            }

            private void OnDayEnding(object sender, DayEndingEventArgs e)
            {
                monitor.Log("Checking for today's birthday NPC...", LogLevel.Debug);

                if (Utility.getTodaysBirthdayNPC(Game1.currentSeason, Game1.dayOfMonth) != null)
                {
                    birthdayNPC = Utility.getTodaysBirthdayNPC(Game1.currentSeason, Game1.dayOfMonth);

                    if (Game1.player.friendshipData.ContainsKey(birthdayNPC.Name))
                    {
                        Game1.addHUDMessage(new HUDMessage($"{birthdayNPC.displayName}'s birthday is today!"));
                    }
                }
                else
                {
                    birthdayNPC = null;
                }


                var nextBirthdayNPCAndGifts = GetNextBirthdayNPCAndGifts();

                if (nextBirthdayNPCAndGifts != null)
                {
                    monitor.Log($"Next birthday is {nextBirthdayNPCAndGifts.Value.Item1}'s in {nextBirthdayNPCAndGifts.Value.Item2} days. They love: {string.Join(", ", nextBirthdayNPCAndGifts.Value.Item3)}", LogLevel.Debug);
                }
                else
                {
                    monitor.Log("No upcoming NPC birthdays found.", LogLevel.Debug);
                }
            }

            public (string NPCName, int DaysUntilBirthday, List<string> LovedGifts)? GetNextBirthdayNPCAndGifts()
            {
                monitor.Log("GetNextBirthdayNPCAndGifts method called.", LogLevel.Debug);

                string[] seasons = new string[] { "spring", "summer", "fall", "winter" };
                monitor.Log("Game1.dayOfMonth: " + Game1.dayOfMonth, LogLevel.Debug);
                for (int daysAhead = 1; daysAhead <= 28; daysAhead++)
                {
                    int dayToCheck = Game1.dayOfMonth + daysAhead;
                    string seasonToCheck = Game1.currentSeason;
                    if (dayToCheck > 28)
                    {
                        dayToCheck -= 28;
                        int seasonIndex = Array.IndexOf(seasons, seasonToCheck);
                        seasonToCheck = seasons[(seasonIndex + 1) % 4];
                    }

                    monitor.Log($"Checking if any NPC has a birthday on {seasonToCheck} {dayToCheck}.", LogLevel.Debug);

                    foreach (NPC npc in Utility.getAllCharacters())
                    {
                        if (npc.Birthday_Season == seasonToCheck && npc.Birthday_Day == dayToCheck)
                        {
                            if (Game1.NPCGiftTastes.ContainsKey(npc.Name))
                            {
                                string giftTastes = Game1.NPCGiftTastes[npc.Name];
                                var lovedGifts = ParseLovedGifts(giftTastes);
                                return (npc.Name, daysAhead, lovedGifts);
                            }
                        }
                    }
                }

                return null;
            }

            public List<string> ParseLovedGifts(string giftTastes)
            {
                monitor.Log("Parsing gift tastes: " + giftTastes, LogLevel.Debug); // Debug log

                var lovedGifts = new List<string>();

                string[] giftTastesParts = giftTastes.Split('/');

                if (giftTastesParts.Length >= 2)
                {
                    string[] ids = giftTastesParts[1].Split(' ');

                    for (int i = 0; i < ids.Length; i++)
                    {
                        string id = ids[i];

                        if (int.TryParse(id, out int idNumber)) // Check if id is a valid number
                        {
                            if (idNumber < 0) // This is a category
                            {
                                var categoryItems = GetItemsInCategory(idNumber);

                                foreach (string item in categoryItems)
                                {
                                    lovedGifts.Add(item);
                                    monitor.Log("Added loved gift: " + item, LogLevel.Debug); // Debug log
                                }
                            }
                            else // This is a specific item
                            {
                                string itemName = GetItemName(idNumber);

                                if (itemName != null)
                                {
                                    lovedGifts.Add(itemName);
                                    monitor.Log("Added loved gift: " + itemName, LogLevel.Debug); // Debug log
                                }
                            }
                        }
                    }
                }

                return lovedGifts;
            }

            public List<string> GetItemsInCategory(int category)
            {
                List<string> items = new List<string>();

                foreach (KeyValuePair<int, string> entry in Game1.objectInformation)
                {
                    string rawData = entry.Value;
                    string[] splitData = rawData.Split('/');

                    if (splitData.Length > 3 && int.TryParse(splitData[3], out int itemCategory) && itemCategory == category)
                    {
                        items.Add(splitData[0]);
                    }
                }

                return items;
            }

            public string GetItemName(int id)
            {
                if (Game1.objectInformation.ContainsKey(id))
                {
                    string rawData = Game1.objectInformation[id];
                    string[] splitData = rawData.Split('/');
                    return splitData[0];
                }
                return null;
            }


        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            lastAlertTime = DateTime.Now;

            this.birthdayReminder = new BirthdayReminder(this.Helper, this.Monitor);
            var nextBirthdayNPCAndGifts = this.birthdayReminder.GetNextBirthdayNPCAndGifts();

            if (nextBirthdayNPCAndGifts.HasValue)
            {
                // Safe to use nextBirthdayNPCAndGifts here
                string npcName = nextBirthdayNPCAndGifts.Value.NPCName;
                int daysUntilBirthday = nextBirthdayNPCAndGifts.Value.DaysUntilBirthday;
                List<string> lovedGifts = nextBirthdayNPCAndGifts.Value.LovedGifts;
            }

            ShowCustomMenu();
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button == SButton.P)
            {
                if (Game1.activeClickableMenu == null)
                {
                    ShowCustomMenu();
                }
                else if (Game1.activeClickableMenu is CustomMenu)
                {
                    Game1.exitActiveMenu();
                }
            }
        }


        private void ShowCustomMenu()
        {
            int menuWidth = (int)(Game1.uiViewport.Width * 0.8);
            int menuHeight = (int)(Game1.uiViewport.Height * 0.8);
            int menuX = (Game1.uiViewport.Width - menuWidth) / 2;
            int menuY = (Game1.uiViewport.Height - menuHeight) / 2;

            // Get the result from GetNextBirthdayNPCAndGifts method
            var nextBirthdayNPCAndGifts = this.birthdayReminder.GetNextBirthdayNPCAndGifts(); //error here


            // Update RemainingTime before showing the menu
            customMenu = new CustomMenu(menuX, menuY, menuWidth, menuHeight, this, mushroomStandingTexture, mushroomWalkingTexture, this.MushroomPositions, Game1.graphics.GraphicsDevice, Monitor, birthdayReminder, nextBirthdayNPCAndGifts);
            customMenu.UpdateRemainingTime();  // call this method here


            if (nextBirthdayNPCAndGifts == null)
            {
                Monitor.Log("NextBirthdayNPCAndGifts is null.", LogLevel.Debug);
                return;
            }


            if (customMenu != null)
            {
                Game1.activeClickableMenu = customMenu;
            }
            else
            {
                Monitor.Log("customMenu is null.", LogLevel.Debug);
            }

            Game1.activeClickableMenu = customMenu;
        }

        public int UpdateRemainingTime(int seconds)
        {
            int remainingTime = interval * 60 - (int)(DateTime.Now - lastAlertTime).TotalSeconds;
            remainingTime -= seconds;

            // Subtract the time spent in the menu
            if (MenuOpenedTime != default)
            {
                remainingTime -= (int)(DateTime.Now - MenuOpenedTime).TotalSeconds;
            }

            return remainingTime;
        }


        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (e.IsMultipleOf((uint)interval * 60) && (DateTime.Now - lastAlertTime).TotalSeconds > interval * 60)
            {
                lastAlertTime = DateTime.Now;

            }

            // Update the remaining time in the custom menu
            if (customMenu != null)
            {
                customMenu.UpdateRemainingTime();
            }
        }

    }
}


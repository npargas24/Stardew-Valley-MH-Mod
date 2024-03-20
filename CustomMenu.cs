using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewMod;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using static StardewMod.ModEntry;
using static StardewValley.Menus.InventoryMenu;

public class CustomMenu : IClickableMenu
{
    private ModEntry modEntry;
    private Texture2D mushroomStandingTexture;
    private Texture2D mushroomWalkingTexture;
    private ClickableTextureComponent closeButton;
    private Texture2D menuTexture;
    private SpriteBatch spriteBatch;
    private int remainingTime;


    private int numberOfIdleMushrooms;
    private int numberOfWalkingMushrooms;
    private string timerText;
    private List<AnimatedSprite> mushrooms;

    private List<Vector2> mushroomPositions;

    private int currentFrame = 0;
    private float frameTimer = 0;
    private int frameWidth;
    private int numberOfFrames = 4;  // Consider making this configurable or obtaining it from the texture
    private int frameHeight; // Add this to capture the height of each frame
    private int animationSpeed = 100; // Adjust this value to change the animation speed
    private Rectangle sourceRect;  // Rectangle to capture the current frame of the texture



    private int mushroomFrameTimer;
    private float mushroomAnimationSpeed;

    private float accumulatedTime = 0.0f;


    private int currentWalkingFrame;
    private int currentIdleFrame = 0;
    private float idleFrameTimer = 0;

    private (string NPCName, int DaysUntilBirthday, List<string> LovedGifts)? nextBirthdayNPCAndGifts;


    private BirthdayReminder birthdayReminder;

    public int RemainingTime { get; set; }

    private Vector2 timerButtonSize;
    private int borderSize;

    private IMonitor monitor;



    public CustomMenu(int x, int y, int width, int height, ModEntry modEntry, Texture2D mushroomStandingTexture, Texture2D mushroomWalkingTexture, List<Vector2> mushroomPositions, GraphicsDevice graphicsDevice, IMonitor monitor, BirthdayReminder birthdayReminder, (string NPCName, int DaysUntilBirthday, List<string> LovedGifts)? nextBirthdayNPCAndGifts)
        : base(x, y, width, height)
    {

        this.modEntry = modEntry;

        this.mushroomStandingTexture = mushroomStandingTexture;
        this.mushroomWalkingTexture = mushroomWalkingTexture;

        frameWidth = mushroomWalkingTexture.Width / numberOfFrames;
        frameHeight = mushroomWalkingTexture.Height;

        this.spriteBatch = new SpriteBatch(graphicsDevice); // Initialize spriteBatch

        // Initialize timerButtonSize and borderSize
        this.timerButtonSize = new Vector2(100, 100);
        this.borderSize = 10;

        this.timerText = "";
        this.mushrooms = new List<AnimatedSprite>();

        this.mushroomPositions = mushroomPositions;

        this.modEntry = modEntry;

        this.monitor = monitor;

        this.birthdayReminder = birthdayReminder;

        this.nextBirthdayNPCAndGifts = nextBirthdayNPCAndGifts;

        this.frameTimer = 0;
        //this.currentFrame = 0;
        this.animationSpeed = 100;
        this.numberOfIdleMushrooms = 10;
        this.numberOfWalkingMushrooms = 4;
        this.mushroomPositions = new List<Vector2>();

        // Calculate the position of the timer box
        int timerBoxX = this.xPositionOnScreen + this.width / 2 - 425;
        int timerBoxY = this.yPositionOnScreen + this.height / 2 - 75;
        int timerBoxWidth = 850;

        // Calculate the positions of the mushrooms
        Vector2 mushroom1Position = new Vector2(timerBoxX - this.frameWidth, timerBoxY + timerBoxWidth / 2);
        Vector2 mushroom2Position = new Vector2(timerBoxX + timerBoxWidth, timerBoxY + timerBoxWidth / 2);

        // Add the positions to the list
        this.mushroomPositions.Add(mushroom1Position);
        this.mushroomPositions.Add(mushroom2Position);




        this.mushroomFrameTimer = 0;
        this.mushroomAnimationSpeed = 500f;

        sourceRect = new Rectangle(0, 0, frameWidth, frameHeight);

        this.closeButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 48, yPositionOnScreen, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f);
        this.menuTexture = Game1.menuTexture;
    }

    public override void update(GameTime time)
    {
        base.update(time);

        // Accumulate the elapsed time
        accumulatedTime += (float)time.ElapsedGameTime.TotalSeconds;

        // Update the remaining time every second
        if (accumulatedTime >= 1)
        {
            UpdateRemainingTime();
            accumulatedTime -= 1;
        }

        // Update the frame timer
        frameTimer += (float)time.ElapsedGameTime.TotalMilliseconds;

        // When the timer exceeds the animation speed, advance to the next frame
        if (frameTimer >= animationSpeed)
        {
            frameTimer = 0;
            currentFrame++;
            if (currentFrame >= numberOfFrames)
            {
                currentFrame = 0;
            }

            // Update the source rectangle to capture the current frame
            sourceRect.X = frameWidth * currentFrame;
        }

        // Update the frame timer for the idle mushrooms
        idleFrameTimer += (float)time.ElapsedGameTime.TotalMilliseconds;

        // When the timer exceeds the animation speed, advance to the next frame
        if (idleFrameTimer >= animationSpeed)
        {
            idleFrameTimer = 0;
            currentIdleFrame++;
            if (currentIdleFrame >= numberOfFrames)
            {
                currentIdleFrame = 0;
            }
        }

        // Update the frame timer for the idle mushrooms
        idleFrameTimer += (float)time.ElapsedGameTime.TotalMilliseconds;

        // When the timer exceeds the animation speed, advance to the next frame
        if (idleFrameTimer >= animationSpeed)
        {
            idleFrameTimer = 0;
            currentIdleFrame++;
            if (currentIdleFrame >= numberOfFrames)
            {
                currentIdleFrame = 0;
            }
        }
    }

    public override void draw(SpriteBatch b)
    {
        var nextBirthdayNPCAndGifts = this.birthdayReminder.GetNextBirthdayNPCAndGifts();

        spriteBatch.Begin();

        IClickableMenu.drawTextureBox(b, this.menuTexture, new Rectangle(0, 256, 60, 60), this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Color.White, 1f, true);



        int offset = 25; // Adjust the offset as needed
        int idleSpacing = (this.width - 2 * offset) / (this.numberOfIdleMushrooms - (int).5); // Calculate the spacing between each mushroom

        for (int i = 0; i < this.numberOfIdleMushrooms; i++)
        {
            b.Draw(this.mushroomStandingTexture,
                   new Vector2(this.xPositionOnScreen + offset + i * idleSpacing, this.yPositionOnScreen + this.height - this.mushroomStandingTexture.Height),
                   new Rectangle(this.currentIdleFrame * this.frameWidth, 0, this.frameWidth, this.mushroomStandingTexture.Height),
                   Color.White);
        }




        // Draw walking mushrooms
        int count = Math.Min(this.numberOfWalkingMushrooms, this.mushroomPositions.Count);
        for (int i = 0; i < count; i++)
        {
            b.Draw(this.mushroomWalkingTexture, this.mushroomPositions[i], new Rectangle(this.currentWalkingFrame * this.frameWidth, 0, this.frameWidth, this.mushroomWalkingTexture.Height), Color.White);
        }



        // Draw the timer box
        IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), this.xPositionOnScreen + this.width / 2 - 425, this.yPositionOnScreen + this.height / 2 - 75, 850, 125, Color.White);

        // Draw the timer text
        string timerMessage = $"Time remaining until your next break: {displayRemainingTime / 60}:{displayRemainingTime % 60:D2}";

        Vector2 timerPosition = new Vector2(xPositionOnScreen + width / 2 - Game1.dialogueFont.MeasureString(timerMessage).X / 2, yPositionOnScreen + height / 2 - Game1.dialogueFont.MeasureString(timerMessage).Y / 2);
        b.DrawString(Game1.dialogueFont, timerMessage, timerPosition, Game1.textColor);


        // Calculate the positions for the mushrooms and the timer
        Vector2 timerTextPosition = new Vector2(xPositionOnScreen + (width / 2), yPositionOnScreen + height / 2);
        Vector2 leftMushroomPosition = new Vector2(xPositionOnScreen + borderSize, yPositionOnScreen + height / 2);
        Vector2 rightMushroomPosition = new Vector2(xPositionOnScreen + width - borderSize - frameWidth, yPositionOnScreen + height / 2);

        string weatherMessage = Game1.isRaining ? "It's raining! It's a good time to go mining." : "It's not raining. You can forage or fish.";


        // Calculate the position of the weather message to be horizontally centered and below the timer message
        Vector2 weatherMessageSize = Game1.dialogueFont.MeasureString(weatherMessage);
        Vector2 weatherMessagePosition = new Vector2(this.xPositionOnScreen + this.width / 2 - weatherMessageSize.X / 2, timerTextPosition.Y + 100); // Adjust the Y coordinate to move the message below the timer

        b.DrawString(Game1.dialogueFont, weatherMessage, weatherMessagePosition, Game1.textColor);

        spriteBatch.Draw(mushroomWalkingTexture, new Vector2(xPositionOnScreen + width / 2, yPositionOnScreen + height - 64), sourceRect, Color.White);

        b.Draw(this.mushroomWalkingTexture, leftMushroomPosition, new Rectangle(this.currentFrame * this.frameWidth, 0, this.frameWidth, this.mushroomWalkingTexture.Height), Color.White);
        b.Draw(this.mushroomWalkingTexture, rightMushroomPosition, new Rectangle(this.currentFrame * this.frameWidth, 0, this.frameWidth, this.mushroomWalkingTexture.Height), Color.White);




        if (nextBirthdayNPCAndGifts != null)
        {
            monitor.Log($"Next birthday is {nextBirthdayNPCAndGifts.Value.Item1}'s in {nextBirthdayNPCAndGifts.Value.Item2} days. They love: {string.Join(", ", nextBirthdayNPCAndGifts.Value.Item3)}", LogLevel.Debug);

            // Create the birthday message
            string birthdayMessage = $"Next birthday is {nextBirthdayNPCAndGifts.Value.Item1}'s in {nextBirthdayNPCAndGifts.Value.Item2} days. They love: {string.Join(", ", nextBirthdayNPCAndGifts.Value.Item3)}";

            // Calculate maximum line width based on your menu width
            int lineWidth = this.width - 50; // Adjust the subtraction value as needed to fit the text within your menu

            // Wrap the text
            string wrappedBirthdayMessage = Game1.parseText(birthdayMessage, Game1.dialogueFont, lineWidth);

            // Calculate the height of the wrapped text
            int textHeight = (int)Game1.dialogueFont.MeasureString(wrappedBirthdayMessage).Y;


            // Calculate the height of the birthday message
            int birthdayMessageHeight = (int)Game1.dialogueFont.MeasureString(wrappedBirthdayMessage).Y;

            // Calculate the position of the birthday message to be horizontally centered and above the timer message
            Vector2 birthdayMessageSize = Game1.dialogueFont.MeasureString(wrappedBirthdayMessage);
            Vector2 birthdayMessagePosition = new Vector2(this.xPositionOnScreen + (this.width - birthdayMessageSize.X) / 2, timerTextPosition.Y - birthdayMessageHeight - 100); // Changed here



            // Split the wrapped text into lines
            string[] lines = wrappedBirthdayMessage.Split('\n');

            // Draw the birthday message
            // Vector2 birthdayMessagePosition = weatherMessagePosition + new Vector2(0, Game1.dialogueFont.LineSpacing);

            // Calculate the total height of the birthday message
            int totalHeight = Game1.dialogueFont.LineSpacing * lines.Length;

            // Subtract the total height from the initial Y position
            //birthdayMessagePosition = new Vector2(this.xPositionOnScreen + (this.width - birthdayMessageSize.X) / 2, weatherMessagePosition.Y + weatherMessageSize.Y - totalHeight + 20);

            foreach (string line in lines)
            {
                Vector2 lineSize = Game1.dialogueFont.MeasureString(line);
                Vector2 linePosition = new Vector2(this.xPositionOnScreen + (this.width - lineSize.X) / 2, birthdayMessagePosition.Y);
                b.DrawString(Game1.dialogueFont, line, linePosition + new Vector2(2f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.dialogueFont, line, linePosition, Game1.textColor);
                birthdayMessagePosition.Y += Game1.dialogueFont.LineSpacing;
            }

        }


        this.closeButton.draw(b);
        this.drawMouse(b);

        //UpdateRemainingTime();
        spriteBatch.End();


    }
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.closeButton.containsPoint(x, y))
        {
            Game1.playSound("bigDeSelect");
            Game1.exitActiveMenu();
            modEntry.MenuOpenedTime = default;
        }
    }

    private int displayRemainingTime;

    public void UpdateRemainingTime()
    {
        displayRemainingTime = modEntry.UpdateRemainingTime(1);
    }
}


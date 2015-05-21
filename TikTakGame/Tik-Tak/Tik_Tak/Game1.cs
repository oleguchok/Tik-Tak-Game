using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Tik_Tak
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Texture2D paddle;
        Vector2 paddlePosition = Vector2.Zero;

        Texture2D ball;
        Vector2 ballPosition = Vector2.Zero;

        bool movingUp, movingLeft;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            movingLeft = true;
            movingUp = true;

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            paddle = Content.Load<Texture2D>("Paddle");
            paddlePosition = new Vector2((graphics.GraphicsDevice.Viewport.Width/2) - (paddle.Width/2),
                graphics.GraphicsDevice.Viewport.Height - 70);

            ball = Content.Load<Texture2D>("Ball");
            ballPosition = new Vector2((graphics.GraphicsDevice.Viewport.Width / 2) - (ball.Width / 2),
                graphics.GraphicsDevice.Viewport.Height - 115);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // TODO: Add your update logic here
            if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Left) &&
                paddlePosition.X >= 0)
            {
                paddlePosition.X -= 3;
            }

            if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Right) &&
                paddlePosition.X <= (graphics.GraphicsDevice.Viewport.Width - paddle.Width))
            {
                paddlePosition.X += 3;
            }

            if (movingUp)
            {
                ballPosition.Y -= 3;
            }
            if(movingLeft)
            {
                ballPosition.X -= 3;
            }
            if(!movingUp)
            {
                ballPosition.Y += 3;
            }
            if(!movingLeft)
            {
                ballPosition.X += 3;
            }

            if (ballPosition.X <= 0 && movingLeft)
                movingLeft = false;
            if (ballPosition.Y <= 0 && movingUp)
                movingUp = false;

            if (ballPosition.X >= (graphics.GraphicsDevice.Viewport.Width - ball.Width)
                    && !movingLeft)
                movingLeft = true;

            if(DetectPaddleBallCollision())
            {
                movingUp = true;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here
            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);
            spriteBatch.Draw(paddle, paddlePosition, Color.White);
            spriteBatch.Draw(ball, ballPosition, Color.White);
            spriteBatch.End();


            base.Draw(gameTime);
        }

        public bool DetectPaddleBallCollision()
        {
            if ((ballPosition.Y + ball.Height) >= paddlePosition.Y &&
                (ballPosition.Y + ball.Height) < (paddlePosition.Y + 4) &&
                (ballPosition.X + ball.Width) > paddlePosition.X &&
                ballPosition.X < (paddlePosition.X + paddle.Width))
                return true;
            else
                return false;
        }
    }
}

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
using Microsoft.Xna.Framework.Net;
using System.Net.Sockets;

namespace Tik_Tak
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Texture2D paddle;
        Vector2 paddlePosition = Vector2.Zero;

        Texture2D ball;
        Vector2 ballPosition = Vector2.Zero;

        TcpClient tcpClient;
        string IP = "127.0.0.1";
        int PORT = 1490;
        int BUFFER_SIZE = 2048;
        byte[] readBuffer;

        bool movingUp, movingLeft;

        enum GameStates
        {
            Menu,
            Playing,
            Pause
        }

        GameStates currentGameState = GameStates.Menu;
        cButton btnPlay;
        
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(IP, PORT);
            tcpClient.NoDelay = true;
            readBuffer = new byte[BUFFER_SIZE];

            tcpClient.GetStream().BeginRead(readBuffer, 0, BUFFER_SIZE, StreamReceived, null);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            movingLeft = true;
            movingUp = true;
            IsMouseVisible = true;
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            paddle = Content.Load<Texture2D>("Paddle");
            paddlePosition = new Vector2((graphics.GraphicsDevice.Viewport.Width/2) - (paddle.Width/2),
                graphics.GraphicsDevice.Viewport.Height - 70);

            ball = Content.Load<Texture2D>("Ball");
            ballPosition = new Vector2((graphics.GraphicsDevice.Viewport.Width / 2) - (ball.Width / 2),
                graphics.GraphicsDevice.Viewport.Height - 115);

            btnPlay = new cButton(Content.Load<Texture2D>("start"), graphics.GraphicsDevice);
            btnPlay.SetPosition(new Vector2(350, 300));
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Update(GameTime gameTime)
        {
            MouseState mouse = Mouse.GetState();

            switch(currentGameState)
            {
                case GameStates.Menu:
                    if (btnPlay.isClicked == true) currentGameState = GameStates.Playing;
                    btnPlay.Update(mouse);
                    break;

                case GameStates.Playing:
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
                    break;
            }

            // TODO: Add your update logic here
            

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here
            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);
            switch (currentGameState)
            {
                case GameStates.Menu:
                    btnPlay.Draw(spriteBatch);
                    break;

                case GameStates.Playing:
                    spriteBatch.Draw(paddle, paddlePosition, Color.White);
                    spriteBatch.Draw(ball, ballPosition, Color.White);
                    break;
            }
            
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

        private void StreamReceived(IAsyncResult ar)
        {
            tcpClient.GetStream().BeginRead(readBuffer, 0, BUFFER_SIZE, StreamReceived, null);
        }
    }
}

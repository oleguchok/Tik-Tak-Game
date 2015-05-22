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
using System.IO;

namespace Tik_Tak
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GameplayObjects player;
        GameplayObjects enemy;

        Texture2D ball;
        Vector2 ballPosition = Vector2.Zero;

        TcpClient tcpClient;
        string IP = "127.0.0.1";
        int PORT = 1490;
        int BUFFER_SIZE = 2048;
        byte[] readBuffer;

        MemoryStream readStream, writeStream;
        BinaryReader reader;
        BinaryWriter writer;

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

            readStream = new MemoryStream();
            writeStream = new MemoryStream();

            reader = new BinaryReader(readStream);
            writer = new BinaryWriter(writeStream);

            tcpClient.GetStream().BeginRead(readBuffer, 0, BUFFER_SIZE, StreamReceived, null);

            player = new GameplayObjects();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            movingLeft = true;
            movingUp = true;
            IsMouseVisible = true;
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            player.Texture = Content.Load<Texture2D>("Paddle");
            player.Position = new Vector2((graphics.GraphicsDevice.Viewport.Width / 2) - (player.Texture.Width / 2),
                graphics.GraphicsDevice.Viewport.Height - 70);

            /*paddle = Content.Load<Texture2D>("Paddle");
            paddlePosition = new Vector2((graphics.GraphicsDevice.Viewport.Width/2) - (paddle.Width/2),
                graphics.GraphicsDevice.Viewport.Height - 70);*/

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
                        player.Position.X >= 0)
                    {
                        player.Position = new Vector2(player.Position.X - 3, player.Position.Y);
                    }

                    if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Right) &&
                        player.Position.X <= (graphics.GraphicsDevice.Viewport.Width - player.Texture.Width))
                    {
                        player.Position = new Vector2(player.Position.X + 3, player.Position.Y);
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

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);
            switch (currentGameState)
            {
                case GameStates.Menu:
                    btnPlay.Draw(spriteBatch);
                    break;

                case GameStates.Playing:
                    if(player != null)
                        spriteBatch.Draw(player.Texture, player.Position, Color.White);
                    if (enemy != null)
                        spriteBatch.Draw(enemy.Texture, enemy.Position, Color.White);
                    spriteBatch.Draw(ball, ballPosition, Color.White);
                    break;
            }
            
            spriteBatch.End();


            base.Draw(gameTime);
        }

        public bool DetectPaddleBallCollision()
        {
            if ((ballPosition.Y + ball.Height) >= player.Position.Y &&
                (ballPosition.Y + ball.Height) < (player.Position.Y + 4) &&
                (ballPosition.X + ball.Width) > player.Position.X &&
                ballPosition.X < (player.Position.X + player.Texture.Width))
                return true;
            else
                return false;
        }

        private void StreamReceived(IAsyncResult ar)
        {
            int bytesRead = 0;

            try
            {
                lock(tcpClient.GetStream())
                {
                    bytesRead = tcpClient.GetStream().EndRead(ar);
                }
            }
            catch(Exception e)
            {
                
            }
            
            if (bytesRead == 0)
            {
                tcpClient.Close();
                return;
            }

            byte[] data = new byte[bytesRead];

            for (int i = 0; i < bytesRead; i++)
                data[i] = readBuffer[i];

            ProcessData(data);

            tcpClient.GetStream().BeginRead(readBuffer, 0, BUFFER_SIZE, StreamReceived, null);
        }

        private void ProcessData(byte[] data)
        {
            readStream.SetLength(0);
            readStream.Position = 0;

            readStream.Write(data, 0, data.Length);
            readStream.Position = 0;

            Protocol p;

            try
            {
                p = (Protocol)reader.ReadByte();
 
                if(p == Protocol.Connected)
                {
                    if (enemy == null)
                    {
                        enemy = new GameplayObjects();
                        enemy.Texture = Content.Load<Texture2D>("PaddleEnemy");
                        enemy.Position = new Vector2((graphics.GraphicsDevice.Viewport.Width / 2) - (enemy.Texture.Width / 2),
                            100);

                        writeStream.Position = 0;
                        writer.Write((byte)Protocol.Connected);
                        SendData(GetDataFromMemoryStream(writeStream));
                    }
                }
                else if (p == Protocol.Disconnected)
                {
                    enemy = null;
                }

            }
            catch(Exception ex)
            {

            }
        }

        public void SendData(byte[] b)
        {
            try
            {
                lock (tcpClient.GetStream())
                {
                    tcpClient.GetStream().BeginWrite(b, 0, b.Length, null, null);
                }
            }
            catch (Exception e)
            {
                
            }
        }

        private byte[] GetDataFromMemoryStream(MemoryStream ms)
        {
            byte[] result;

            lock (ms)
            {
                int bytesWritten = (int)ms.Position;
                result = new byte[bytesWritten];

                ms.Position = 0;
                ms.Read(result, 0, bytesWritten);
            }

            return result;
        }
    }
}

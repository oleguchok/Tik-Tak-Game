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
using System.Threading;
using XNAGameConsole;

namespace Tik_Tak
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GameplayObjects player;
        GameplayObjects enemy;
        GameplayObjects gameOver;

        Texture2D ball;
        Vector2 ballPosition = Vector2.Zero;
        Texture2D pause;
        Rectangle pauseRectangle;

        TcpClient tcpClient;
        public string IP;
        public int PORT;
        int BUFFER_SIZE = 2048;
        byte[] readBuffer;
        MemoryStream readStream, writeStream;
        BinaryReader reader;
        BinaryWriter writer;

        bool movingUp, movingLeft;
        bool enemyConnected = false;
        bool host = false;

        GameStates currentGameState = GameStates.Playing;
        
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {  
            readStream = new MemoryStream();
            writeStream = new MemoryStream();

            reader = new BinaryReader(readStream);
            writer = new BinaryWriter(writeStream);            

            player = new GameplayObjects();
            enemy = new GameplayObjects();
            gameOver = new GameplayObjects();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            movingLeft = true;
            movingUp = true;
            IsMouseVisible = true;
            spriteBatch = new SpriteBatch(GraphicsDevice);            

            player.Texture = Content.Load<Texture2D>("Paddle");
            player.Position = new Vector2((graphics.GraphicsDevice.Viewport.Width / 2) - (player.Texture.Width / 2),
                graphics.GraphicsDevice.Viewport.Height - 50);

            enemy.Texture = Content.Load<Texture2D>("PaddleEnemy");

            ball = Content.Load<Texture2D>("Ball");
            ballPosition = new Vector2((graphics.GraphicsDevice.Viewport.Width / 2) - (ball.Width / 2),
                graphics.GraphicsDevice.Viewport.Height - 95);

            pause = Content.Load<Texture2D>("pause");
            pauseRectangle = new Rectangle(0, 0, pause.Width, pause.Height);

            gameOver.Texture = Content.Load<Texture2D>("gameover");
            gameOver.Position = new Vector2(0,0);

            tcpClient = new TcpClient();
            tcpClient.Connect(IP, PORT);
            tcpClient.NoDelay = true;
            readBuffer = new byte[BUFFER_SIZE];
            tcpClient.GetStream().BeginRead(readBuffer, 0, BUFFER_SIZE, StreamReceived, null);
        }

        protected override void UnloadContent()
        {

        }

        protected override void Update(GameTime gameTime)
        {
            MouseState mouse = Mouse.GetState();

            switch(currentGameState)
            {
                case GameStates.Playing:
                    if (Keyboard.GetState().IsKeyDown(Keys.Space))
                        currentGameState = GameStates.Pause;

                    Vector2 iPosition = new Vector2(player.Position.X, player.Position.Y);

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

                    Vector2 nPosition = new Vector2(player.Position.X, player.Position.Y);
                    Vector2 delta = Vector2.Subtract(nPosition, iPosition);
                    Vector2 iBallPosition = new Vector2(ballPosition.X, ballPosition.Y);
                    if(host)
                    {
                        if (movingUp)
                        {
                            ballPosition.Y -= 2;
                        }
                        if (movingLeft)
                        {
                            ballPosition.X -= 2;
                        }
                        if (!movingUp)
                        {
                            ballPosition.Y += 2;
                        }
                        if (!movingLeft)
                        {
                            ballPosition.X += 2;
                        }

                        if (ballPosition.X <= 0 && movingLeft)
                            movingLeft = false;
                        
                        if (ballPosition.X >= (graphics.GraphicsDevice.Viewport.Width - ball.Width)
                                && !movingLeft)
                            movingLeft = true;

                        if (DetectPaddleBallCollision(player))
                        {
                            movingUp = true;
                        }
                        if (DetectEnemyPaddleBallCollision(enemy))
                        {
                            movingUp = false;
                        }
                        Vector2 nBallPosition = new Vector2(ballPosition.X, ballPosition.Y);
                        Vector2 bDelta = Vector2.Subtract(nBallPosition, iBallPosition);
                        writeStream.Position = 0;
                        writer.Write((byte)Protocol.Move);
                        writer.Write(bDelta.X);
                        writer.Write(bDelta.Y);
                        writer.Write(delta.X);
                        SendData(GetDataFromMemoryStream(writeStream));
                    }
                    else
                    {                        
                        float a = 0;
                        writeStream.Position = 0;
                        writer.Write((byte)Protocol.Move);
                        writer.Write(a);
                        writer.Write(a);
                        writer.Write(delta.X);
                        SendData(GetDataFromMemoryStream(writeStream));
                    }

                    if (ballPosition.Y < -10 || ballPosition.Y > 800)
                        currentGameState = GameStates.GameOver;
                    break;     
               
                case GameStates.Pause:
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        currentGameState = GameStates.Playing;
                        writeStream.Position = 0;
                        writer.Write((byte)Protocol.Continue);
                        SendData(GetDataFromMemoryStream(writeStream));
                    }
                    else
                    {
                        writeStream.Position = 0;
                        writer.Write((byte)Protocol.Pause);
                        SendData(GetDataFromMemoryStream(writeStream));
                    }
                    break;
                case GameStates.GameOver:
                    writeStream.Position = 0;
                    writer.Write((byte)Protocol.GameOver);
                    SendData(GetDataFromMemoryStream(writeStream));
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
                case GameStates.Playing:
                    if(player != null)
                        spriteBatch.Draw(player.Texture, player.Position, Color.White);
                    if (enemyConnected)
                    {
                        spriteBatch.Draw(enemy.Texture, enemy.Position, Color.White);
                        spriteBatch.Draw(ball, ballPosition, Color.White);
                    }
                    break;

                case GameStates.Pause:
                    spriteBatch.Draw(pause, pauseRectangle, Color.White);
                    break;

                case GameStates.GameOver:
                    spriteBatch.Draw(gameOver.Texture, gameOver.Position, Color.White);
                    break;
            }
            
            spriteBatch.End();


            base.Draw(gameTime);
        }

        public bool DetectPaddleBallCollision(GameplayObjects paddle)
        {
            if ((ballPosition.Y + ball.Height) >= paddle.Position.Y &&
                (ballPosition.Y + ball.Height) < (paddle.Position.Y + 4) &&
                (ballPosition.X + ball.Width) > paddle.Position.X &&
                ballPosition.X < (paddle.Position.X + paddle.Texture.Width))
                return true;
            else
                return false;
        }

        public bool DetectEnemyPaddleBallCollision(GameplayObjects paddle)
        {
            if ((ballPosition.Y) <= (paddle.Position.Y + paddle.Texture.Height) &&
                (ballPosition.Y) > (paddle.Position.Y +paddle.Texture.Height - 4) &&
                (ballPosition.X + ball.Width) > paddle.Position.X &&
                ballPosition.X < (paddle.Position.X + paddle.Texture.Width))
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

        bool was = true;

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
                    if (!enemyConnected)
                    {
                        enemyConnected = true;
                        enemy.Position = new Vector2((graphics.GraphicsDevice.Viewport.Width / 2) - (enemy.Texture.Width / 2),
                            50 - enemy.Texture.Height);

                        writeStream.Position = 0;
                        writer.Write((byte)Protocol.Connected);
                        SendData(GetDataFromMemoryStream(writeStream));
                    }
                    else host = true;
                }
                else if (p == Protocol.Disconnected)
                {
                    enemyConnected = false;
                }
                else if (p == Protocol.Move)
                {
                    float bx = reader.ReadSingle();
                    float by = reader.ReadSingle();
                    float px = reader.ReadSingle();
                    if (host)
                    {
                        ballPosition = new Vector2(ballPosition.X + bx, ballPosition.Y + by);                        
                    }
                    else
                    {
                        
                        if (was)
                        {
                            float dy = ballPosition.Y + by;
                            float dx = ballPosition.X + bx;
                            float deltax = Math.Abs(ballPosition.X + bx - graphics.GraphicsDevice.Viewport.Width / 2);
                            float deltay = Math.Abs(ballPosition.Y + by - graphics.GraphicsDevice.Viewport.Height / 2);
                            if (dx > graphics.GraphicsDevice.Viewport.Width / 2)
                                dx = dx - deltax * 2;
                            else if (dx < graphics.GraphicsDevice.Viewport.Width / 2)
                                dx = dx + deltax * 2;
                            if (dy > graphics.GraphicsDevice.Viewport.Height / 2)
                                dy = dy - deltay * 2;
                            else if (dy < graphics.GraphicsDevice.Viewport.Height / 2)
                                dy = dy + deltay * 2;
                            ballPosition = new Vector2(dx, dy);
                            was = false;
                        }
                        else
                            ballPosition = new Vector2(ballPosition.X - bx, ballPosition.Y - by);
                    }
                    enemy.Position = new Vector2(enemy.Position.X - px, enemy.Position.Y);
                    
                }
                else if (p == Protocol.Pause)
                {
                    currentGameState = GameStates.Pause;
                }
                else if (p == Protocol.Continue)
                {
                    currentGameState = GameStates.Playing;
                }
                else if (p == Protocol.GameOver)
                {
                    currentGameState = GameStates.GameOver;
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

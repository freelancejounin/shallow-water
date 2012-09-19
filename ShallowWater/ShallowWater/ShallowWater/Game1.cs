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

namespace ShallowWater
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;

        float aspectRatio;
        Vector3 cameraPosition;
        Matrix viewMatrix;
        Matrix projectionMatrix;

        Model cube;

        int poolLength = 100;
        int poolWidth = 100;
        Vector3[][] cubePositions;
        Vector3[][] positionProperties;
        Vector3[][] propertyChanges;

        float defaultHeight = 0.5f;
        float blockWidth = 75f;

        Random rand = new Random();

        Vector2 mousePosition = Vector2.Zero;
        Vector2 targetPosition = Vector2.Zero;
        
        // tweaking numbers
        float g = 0.03f;
        float b = 0.2f;

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
            // set up the camera
            aspectRatio = graphics.GraphicsDevice.Viewport.AspectRatio;
            cameraPosition = new Vector3(0.0f, -2500.0f, 2500.0f);
            viewMatrix = Matrix.CreateLookAt(cameraPosition, Vector3.Zero, Vector3.Up);
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                        MathHelper.ToRadians(45.0f), aspectRatio,
                        1.0f, 10000.0f);

            // mouse disappearing is irritating
            this.IsMouseVisible = true;

            // set up the pool
            cubePositions = new Vector3[poolLength][];
            positionProperties = new Vector3[poolLength][];
            propertyChanges = new Vector3[poolLength][];
            for (int i = 0; i < poolLength; i++)
            {
                cubePositions[i] = new Vector3[poolWidth];
                positionProperties[i] = new Vector3[poolWidth];
                propertyChanges[i] = new Vector3[poolWidth];
                for (int j = 0; j < poolWidth; j++)
                {

                    float thisHeight = 0;

                    // set position of this cube - the offset is to put the center of the pool in the
                    // center of the screen
                    cubePositions[i][j] = new Vector3(j * blockWidth - (poolWidth / 2 * blockWidth),
                        i * blockWidth - (poolLength / 2 * blockWidth),
                        0);
                    // set u velocity, v velocity, and height for this position
                    positionProperties[i][j] = new Vector3(0, 0, thisHeight);
                }
            }


            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            cube = Content.Load<Model>("Meshes\\UnitCube");
            font = Content.Load<SpriteFont>("SpriteFont1");
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
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here
            MouseState mState = Mouse.GetState();
            if (mState.LeftButton == ButtonState.Pressed)
            {
                mousePosition = new Vector2(mState.X, mState.Y);
                Ray mouseRay = CalculateCursorRay();
                targetPosition = RayIntersection(mouseRay);
                if (targetPosition.X != -1)
                {
                    AddForceAtPosition(targetPosition);
                }

            }
            if (mState.RightButton == ButtonState.Pressed)
            {
                targetPosition = Vector2.Zero;
            }

            UpdatePool(gameTime.ElapsedGameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            // this is important so new cubes don't overwrite previously drawn cubes
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            // TODO: Add your drawing code here
            Matrix[] transforms = new Matrix[cube.Bones.Count];
            cube.CopyAbsoluteBoneTransformsTo(transforms);

            
            //cubePositions[i][j] = new Vector3(j * blockWidth, i * blockWidth, 0);

            foreach (ModelMesh mesh in cube.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.View = viewMatrix;
                    effect.Projection = projectionMatrix;

                    for (int i = 0; i < poolLength; i++)
                    {
                        for (int j = 0; j < poolWidth; j++)
                        {
                            effect.World = transforms[mesh.ParentBone.Index] * Matrix.CreateScale(blockWidth / 2,blockWidth / 2, (positionProperties[i][j].Z + 1) * blockWidth)  // the unitcube model is length/width/height 2
                            * Matrix.CreateTranslation(cubePositions[i][j].X,cubePositions[i][j].Y,0);
                            // Draw the mesh
                            mesh.Draw();
                        }                        
                    }
                }
            }


            String targetLocation = "X: " + targetPosition.X + ", Y: " + targetPosition.Y;

            spriteBatch.Begin();
            spriteBatch.DrawString(font, targetLocation, new Vector2(20, 80), Color.White);
            spriteBatch.End();                    
            
            base.Draw(gameTime);
        }



        public Ray CalculateCursorRay()
        {
            // create 2 positions in screenspace using the cursor position. 0 is as
            // close as possible to the camera, 1 is as far away as possible.
            Vector3 nearSource = new Vector3(mousePosition, 0f);
            Vector3 farSource = new Vector3(mousePosition, 1f);

            // use Viewport.Unproject to tell what those two screen space positions
            // would be in world space. we'll need the projection matrix and view
            // matrix, which we have saved as member variables. We also need a world
            // matrix, which can just be identity.
            Vector3 nearPoint = GraphicsDevice.Viewport.Unproject(nearSource,
                projectionMatrix, viewMatrix, Matrix.Identity);

            Vector3 farPoint = GraphicsDevice.Viewport.Unproject(farSource,
                projectionMatrix, viewMatrix, Matrix.Identity);

            // find the direction vector that goes from the nearPoint to the farPoint
            // and normalize it....
            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();

            // and then create a new ray using nearPoint as the source.
            return new Ray(nearPoint, direction);
        }

        public Vector2 RayIntersection(Ray cursorRay)
        {
            for (int i = 0; i < poolLength; i++)
            {
                for (int j = 0; j < poolWidth; j++)
                {
                    Vector3 center = cubePositions[i][j];
                    Vector3 min = new Vector3(center.X - blockWidth / 2, center.Y - blockWidth / 2, center.Z - blockWidth / 2);
                    Vector3 max = new Vector3(center.X + blockWidth / 2, center.Y + blockWidth / 2, center.Z + blockWidth / 2);

                    BoundingBox box = new BoundingBox(min, max);
                    if (cursorRay.Intersects(box) != null)
                    {
                        return new Vector2(j, i);                        
                    }
                }
            }

            return new Vector2(-1,-1);
        }

        public void AddForceAtPosition(Vector2 target)
        {
            if (((target.X < 3) || target.X >= (poolWidth - 3)) || ((target.Y < 2) || (target.Y >= poolLength - 2)))
                return;
            
            float heightToSet = 5.0f;

            positionProperties[(int)target.Y][(int)target.X].Z = heightToSet;
            positionProperties[(int)target.Y][(int)target.X + 1].Z = heightToSet;
            positionProperties[(int)target.Y-1][(int)target.X].Z = heightToSet;
            positionProperties[(int)target.Y-1][(int)target.X + 1].Z = heightToSet;
        }

        public void UpdatePool(TimeSpan timespan)
        {
            Vector3 me;
            Vector3 left;
            Vector3 right;
            Vector3 up;
            Vector3 down;
            float maxVelocity = 0;
            float ticks = timespan.Ticks / 100000;
            float distanceBetween = 1f;

            // calculate all the changes
            for (int i = 0; i < poolLength; i++)
            {
                for (int j = 0; j < poolWidth; j++)
                {
                    me = positionProperties[i][j];                    
                    
                    float horizontalDifference;
                    float verticalDifference;

                    // do left/right with border checks
                    if (j - 1 < 0)
                    {
                        propertyChanges[i][j].X *= 0;
                        right = positionProperties[i][j + 1];
                        horizontalDifference = 0;
                    }
                    else if (j + 1 > poolWidth - 1)
                    {
                        propertyChanges[i][j].X *= 0;
                        left = positionProperties[i][j - 1];
                        horizontalDifference = 0;
                    }
                    else
                    {
                        left = positionProperties[i][j - 1];
                        right = positionProperties[i][j + 1];
                        propertyChanges[i][j].X = ticks * (-g * heightCentralDifference(right.Z, left.Z) - b * me.X);
                        horizontalDifference = -(right.X * (defaultHeight + right.Z) - left.X * (defaultHeight + left.Z)) / 2 * distanceBetween;
                    }

                    if (i - 1 < 0)
                    {
                        propertyChanges[i][j].Y *= 0;
                        up = positionProperties[i+1][j];
                        verticalDifference = 0;
                    }
                    else if (i + 1 > poolLength - 1)
                    {
                        propertyChanges[i][j].Y *= 0;
                        down = positionProperties[i - 1][j];
                        verticalDifference = 0;
                    }
                    else
                    {
                        up = positionProperties[i + 1][j];
                        down = positionProperties[i - 1][j];
                        propertyChanges[i][j].Y = ticks * (-g * heightCentralDifference(up.Z,down.Z) - b * me.Y);
                        verticalDifference = -(up.Y * (defaultHeight + up.Z) - down.Y * (defaultHeight + down.Z)) / 2 * distanceBetween;
                    }

                    propertyChanges[i][j].Z = ticks * (horizontalDifference + verticalDifference);

                    Vector3 check = propertyChanges[i][j];
                    if (Math.Abs(check.X) > maxVelocity)
                        maxVelocity = Math.Abs(check.X);
                    if (Math.Abs(check.Y) > maxVelocity)
                        maxVelocity = Math.Abs(check.Y);
                }
            }

            // now make the changes
            for (int i = 0; i < poolLength; i++)
            {
                for (int j = 0; j < poolWidth; j++)
                {
                    positionProperties[i][j] += propertyChanges[i][j];
                }
            }
            /*
            if (maxVelocity * ticks > 0.0009)
            {
                UpdatePool(new TimeSpan((long)ticks / 100000000));
            }
            */
        }

        public float heightCentralDifference(float heightA, float heightB)
        {
            return (heightA - heightB) / 2;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SkinnedModel;

namespace Firebird
{
	/// <summary>
	/// Stores model information.
	/// </summary>
	public class GModel
	{
		public Model model;
		public SkinningData skinningData;
		public Texture2D Texture;
		public Vector3[] MaterialColors;
		public float Emissive; // deprecated
		public Texture2D EmissiveTexture;
		public bool IsWaterfall;

		public string TexturePath;
		public string EmissivePath;

		public GModel()
		{ }

		/// <summary>
		/// Copy constructor. Creates a deep copy of another GModel.
		/// </summary>
		/// <param name="gm">GModel to clone.</param>
		public GModel(GModel gm)
		{
			model = gm.model;
			skinningData = gm.skinningData;
			Texture = gm.Texture;
			MaterialColors = gm.MaterialColors != null ? (Vector3[])gm.MaterialColors.Clone() : null;
			Emissive = gm.Emissive;
			EmissiveTexture = gm.EmissiveTexture;
			IsWaterfall = gm.IsWaterfall;
		}

		public override bool Equals(object obj)
		{
			GModel gm = obj as GModel;

			return model == gm.model
				&& TexturePath == gm.TexturePath
				&& EmissivePath == gm.EmissivePath;
		}

		public override string ToString()
		{
			return model.ToString();
		}
	}

	public abstract class GraphicsManager
	{
		#region Fields

		/// <summary>
		/// Describes a stage of the rendering pipeline.
		/// </summary>
		protected enum RenderState
		{
			/// <summary>
			/// Deferred pre-pass.
			/// </summary>
			Deferred,
			/// <summary>
			/// Shadow-mapping.
			/// </summary>
			Shadow,
			/// <summary>
			/// Lighting 
			/// </summary>
			Lighting,
			/// <summary>
			/// Not rendering
			/// </summary>
			None
		}

		/// <summary>
		/// Renderer's current pipeline stage.
		/// </summary>
		protected RenderState renderState = RenderState.None;

		/// <summary>
		/// Graphics Device copy.
		/// </summary>
		public GraphicsDevice Device { get; private set; }
		protected GraphicsDeviceManager gdm;

		protected ContentManager content;
		/// <summary>
		/// Content Manager copy/
		/// </summary>
		public ContentManager Content { get { return content; } }

		// Drawing
		protected GameTime gameTime = null; // set every frame in StartDrawing
		protected CameraManager camera = null; // set every frame in StartDrawing

		// Meshes
		protected Dictionary<string, List<GModel>> models = new Dictionary<string, List<GModel>>();

		// Lighting
		public Color Ambient { get; set; }

        /// <summary>
        /// Outer background color. Can be values from 0-4, where values from 0-1 are standard
		/// color values and 1-4 are HDR (brighter than white) colors that will bloom.
        /// </summary>
        public Vector3 OuterBackgroundColor { get; set; }

		/// <summary>
		/// Center background color. Can be values from 0-4, where values from 0-1 are standard
		/// color values and 1-4 are HDR (brighter than white) colors that will bloom.
		/// </summary>
		public Vector3 InnerBackgroundColor { get; set; }

		/// <summary>
		/// Toggles whether fog is enabled.
		/// </summary>
		public bool EnableFog { get; set; }

		/// <summary>
		/// Start depth of fog (0-1).
		/// </summary>
		public float FogBegin { get; set; }

		/// <summary>
		/// Depth at which elements have completely blended into
		/// the background.
		/// </summary>
		public float FogEnd { get; set; }

		/// <summary>
		/// True if the current graphics profile is HiDef.
		/// </summary>
		public bool IsHiDef { get { return Device.GraphicsProfile == GraphicsProfile.HiDef; } }

		/// <summary>
		/// True if the current graphics profile is Reach.
		/// </summary>
		public bool IsReach { get { return Device.GraphicsProfile == GraphicsProfile.Reach; } }

		#endregion

		#region Initialization and Loading

		/// <summary>
		/// Constructs a new GraphicsManager. Put in Game1's constructor.
		/// </summary>
		public GraphicsManager(GraphicsDeviceManager gdmArg, ContentManager cmg)
		{
			gdm = gdmArg;
            this.content = cmg;
		}

		/// <summary>
		/// Constructs a new GraphicsManager. Put in Game1's constructor.
		/// </summary>
		public GraphicsManager(GraphicsDevice device, ContentManager cmg)
		{
			Device = device;
			this.content = cmg;
		}

		/// <summary>
		/// Passes the graphics device manager to GraphicsManager so it can
		/// initialize itself. Put in Game.Initialize
		/// </summary>
		/// <param name="gdmArg">Game's GraphicsDeviceManager</param>
		public void Initialize()
		{
			// something here later?
		}

		/// <summary>
		/// Sets important Graphics Device Manager flags as the screen is
		/// being created. Put in Game.LoadContent.
		/// <param name="width">Resolution width</param>
		/// <param name="height">Resolution height</param>
		/// </summary>
		public void LoadGraphics(int width, int height)
		{
			if (gdm != null)
				Device = gdm.GraphicsDevice; // GraphicsDevice isn't created until LoadContent

			// Load device parameters
#if GAME
			PresentationParameters pp = Device.PresentationParameters;

			// modify GDM settings
			IndependentResolutionRendering.Resolution.Init(ref gdm);
			IndependentResolutionRendering.Resolution.SetVirtualResolution(
				Game1.ScreenWidth,
				Game1.ScreenHeight);

			SetResolution(width, height);

			gdm.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
			gdm.SynchronizeWithVerticalRetrace = false;
			gdm.ApplyChanges();
#endif

			// Manually Load models with external textures
			LoadModel("Testing\\boundingbox", null);
			LoadModel("Testing\\icosphere", null);
			
#if GAME
			LoadModel(@"Game/Character/Nox/shield", @"Game\Character\Nox\NoxShieldConcept2");
			LoadModel(@"Game/Character/Caleo/shield", @"Game\Character\Caleo\CaleoShieldConcept2");
			LoadModel(@"Game/Character/Vitreus/shield", @"Game\Character\Vitreus\VitreusShieldConcept");
#endif

			LoadEffects();
		}

#if GAME
		/// <summary>
		/// Sets optimal resolution. Uses default resolution, unless it has a higher pixel
		/// count than the one passed.
		/// </summary>
		/// <param name="width">Target resolution width</param>
		/// <param name="height">Target resolution height</param>
		private void SetResolution(int width, int height)
		{
#if XBOX
			// Choose the minimum of the default vs. target resolution.

			int maxPixelCount = width * height;
			int defaultPixelCount =
				Device.Adapter.CurrentDisplayMode.Width *
				Device.Adapter.CurrentDisplayMode.Height;

			if (defaultPixelCount <= maxPixelCount)
			{
				width = Device.Adapter.CurrentDisplayMode.Width;
				height = Device.Adapter.CurrentDisplayMode.Height;
			}
#endif
			IndependentResolutionRendering.Resolution.SetResolution(
				width,
				height,
				Game1.finalize);
		}
#endif

		/// <summary>
		/// Loads shader content into graphics memory.
		/// </summary>
		protected abstract void LoadEffects();

		/// <summary>
		/// Loads a model from a file and places it in the models dictionary.
		/// </summary>
		/// <param name="fileName">Path to model</param>
		/// <param name="texture">Texture override (null for default)</param>
		protected abstract GModel LoadModel(string fileName, string texture);

		/// <summary>
		/// Loads a model from a file and places it in the models dictionary.
		/// </summary>
		/// <param name="fileName">Path to model</param>
		/// <param name="texture">Texture override (null for default)</param>
		/// <param name="emissiveTexture">Emissive texture (or null)</param>
		protected abstract GModel LoadModel(string fileName, string texture, string emissiveTexture);

		/// <summary>
		/// Finds a model. If the model has already been loaded by GraphicsManager, it
		/// returns the loaded model. If not, the model is loaded with a BasicEffect
		/// and returned.
		/// </summary>
		/// <param name="fileName">Path to model</param>
		/// <returns>The model at a given file name</returns>
		public GModel GetModel(string fileName)
		{
			return LoadModel(fileName, null);
		}

		/// <summary>
		/// Finds a model. If the model has already been loaded by GraphicsManager, it
		/// returns the loaded model. If not, the model is loaded with a BasicEffect
		/// and returned.
		/// </summary>
		/// <param name="fileName">Path to model</param>
		/// <param name="texture">Special texture, or null for default</param>
		/// <returns>The model at a given file name</returns>
		public GModel GetModel(string fileName, string texture)
		{
			return LoadModel(fileName, texture);
		}

		/// <summary>
		/// Finds a model. If the model has already been loaded by GraphicsManager, it
		/// returns the loaded model. If not, the model is loaded with a BasicEffect
		/// and returned.
		/// </summary>
		/// <param name="fileName">Path to model</param>
		/// <returns>The model at a given file name</returns>
		public GModel GetModel(string fileName, string texture, string emissiveTexture)
		{
			return LoadModel(fileName, texture, emissiveTexture);
		}

		/// <summary>
		/// Called in the editor when the view is reset.
		/// </summary>
		public abstract void Reset(object sender, EventArgs args);

		#endregion

		#region Lighting

		/// <summary>
		/// Adds a dynamic light to the scene. Will be rendered every
		/// frame until RemoveLight is called.
		/// </summary>
		/// <param name="light">Light</param>
		public abstract void AddDynamicLight(Light light);

		/// <summary>
		/// Adds a dynamic light to the scene. Will be rendered every frame
		/// until RemoveLight is called. Has priority over normal lights.
		/// </summary>
		/// <param name="light"></param>
		public abstract void AddSceneLight(Light light);

		/// <summary>
		/// Removes a light from the scene.
		/// </summary>
		/// <param name="light">Light</param>
		public abstract void RemoveLight(Light light);

		/// <summary>
		/// Removes all 
		/// </summary>
		public abstract void RemoveAllDynamicLights();

		/// <summary>
		/// Clears all lights from the scene.
		/// </summary>
		public abstract void RemoveAllLights();

		public abstract Light ShadowCastingLight { get; }

		#endregion

		#region Drawing

		/// <summary>
		/// Called at the beginning of the drawing phase; starts deferred
		/// rendering stage.
		/// </summary>
		/// <param name="gameTime">Current game time</param>
		public abstract void StartDrawing(GameTime gameTime, CameraManager camera);

        /// <summary>
        /// Used for rendering characters on menu screens.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="camera"></param>
		public abstract void StartDrawingCharacters(GameTime gameTime, CameraManager camera, RenderTarget2D rt, Viewport viewport);

        /// <summary>
        /// used for rendering shadows
        /// </summary>
        public abstract void StopDeferredStartShadow();

		/// <summary>
		/// Stop the drawing sequence. Applies any post effects and processing,
		/// then resets the GraphicsManager state for the next frame.
		/// </summary>
		public abstract void StopShadowStartLighting();

        /// <summary>
        /// Finishes drawing a portrait by rendering temporary data into a render target.
        /// </summary>
        /// <param name="rt"></param>
		public abstract void FinalizePortrait(RenderTarget2D rt);

        /// <summary>
        /// Used to finalize the drawing process.
        /// </summary>
		public abstract void StopDrawing();

		/// <summary>
		/// Draws a model with a basic shader.
		/// </summary>
		/// <param name="model">Model</param>
		/// <param name="position">Position</param>
		/// <param name="rotation">Model rotation</param>
		public abstract void DrawModel(GModel gmodel, Vector3 position, Quaternion rotation, float scale);

        /// <summary>
        /// Draws a model with a basic shader.
        /// </summary>
        /// <param name="model">Model</param>
        /// <param name="position">Position</param>
        /// <param name="rotation">Model rotation</param>
        public abstract void DrawModel(GModel gmodel, Vector3 position, Quaternion rotation, float scale, Matrix[] bones);

		/// <summary>
		/// Draws a model with a basic shader.
		/// </summary>
		/// <param name="model">Model</param>
		/// <param name="position">Position</param>
		/// <param name="rotation">Model rotation</param>
		/// <param name="scale">Model scaling</param>
		public abstract void DrawModel(GModel gmodel, Vector3 position, Quaternion rotation, Vector3 scale, Matrix[] bones);

		/// <summary>
		/// Uses geometry instancing to draw several models. Models must have one
		/// ModelMesh with a single ModelMeshPart.
		/// </summary>
		/// <param name="model">Model</param>
		/// <param name="transforms">Transform data</param>
		/// <param name="nPlatforms">Number of transforms</param>
		public abstract void DrawInstancedPlatforms(GModel model, Matrix[] transforms, int nPlatforms);

		/// <summary>
		/// Draws a character's shield.
		/// </summary>
		/// <param name="shieldModel">Model</param>
		/// <param name="position">Center position</param>
		/// <param name="scale">Scale</param>
		public abstract void DrawShield(GModel shieldModel, Vector3 position, float scale);

		/// <summary>
		/// Draws a bounding box.
		/// </summary>
		/// <param name="bb">Bounding Box</param>
		/// <param name="position">Center position of the bounding box</param>
		/// <param name="rotation">Box rotation</param>
		public abstract void DrawBoundingBox(BoundingBox bb, Vector3 position);
        
		/// <summary>
		/// Draws a wireframe sphere.
		/// </summary>
		/// <param name="radius">Sphere radius</param>
		/// <param name="position">Sphere position</param>
		public abstract void DrawBoundingSphere(float radius, Vector3 position);
		#endregion
	}
}

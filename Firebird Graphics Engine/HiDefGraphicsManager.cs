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
    /// Management class for all drawing and all actual graphics code. Owns
    /// and operates the GraphicsDeviceManager.
    /// </summary>
    public class HiDefGraphicsManager : GraphicsManager
	{
		#region Fields

		// Deferred rendering
		private const int nRenderTargets = 3;
		private RenderTargetBinding[] deferredTargets;

		// Effects
		private Effect deferredEffect; // used to deferr rendering to render targets
        private Effect deferredSkinnedEffect; // render skinned models (no texture currently)
		private Effect deferredTextureEffect; // deferred rendering with textures
		private Effect forwardUnshadedEffect; // used for bounding boxes/spheres
        private Effect portraitEffect; // post-processing effect used for character portraits
		private Effect instanceEffect; // supports hardware instancing
		// Post-processing effects
		private Effect clearEffect; // clears MRTs
		private Effect edgeEffect; // edge highlighting
		private Effect ambientLightEffect; // ambient light contribution
		private Effect pointLightEffect; // point light contribution
		private Effect directionalLightEffect; // directional light contribution
		private Effect shieldEffect; // transparent shield effect
		private Effect fogEffect;	// distance-based fog postprocess
		// HDR effects
		private Effect hdrEffect; // HDR effect

		// HDR
		private RenderTarget2D[] bloomDownSampleBuffers; // downsample buffers for bloom
		private RenderTarget2D bloomAccumulationBuffer; // temporary buffer storage

		// Lighting
		private List<Light> sceneLights; // lights imported from the map editor
		private List<Light> dynamicLights; // lights from dynamic objects, such as spells
		private RenderTarget2D lightAccumulationBuffer; // accumulation buffer for lighting phase

		// Shadows
		private RenderTarget2D lightMap;
		private Matrix lightViewProjection;

		// Instancing
		private DynamicVertexBuffer instanceBuffer = null;
		private static VertexDeclaration instanceDeclaration = new VertexDeclaration
		(
			new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
			new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
			new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
			new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3)
		);

		// Waterfall timer
		private float waterfallTimer;
		private const float waterfallPeriod = 2.0f;

		// Testing
		private int nDrawCalls;

		#endregion

		#region Initialization and Loading

		/// <summary>
		/// Constructs a new GraphicsManager. Put in Game1's constructor.
		/// </summary>
		public HiDefGraphicsManager(GraphicsDeviceManager gdmArg, ContentManager cmg)
			: base(gdmArg, cmg)
		{
			// lighting
			Ambient = new Color(64, 64, 64);
            OuterBackgroundColor = new Vector3(0.25f, 0.25f, 0.25f);
			InnerBackgroundColor = new Vector3(-1, 0, 0);
			EnableFog = false;
			FogBegin = 0;
			FogEnd = 1;
			dynamicLights = new List<Light>();
			sceneLights = new List<Light>();
            gdm.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
			waterfallTimer = 0;
		}

		/// <summary>
		/// Constructs a new GraphicsManager. Put in Game1's constructor.
		/// </summary>
		public HiDefGraphicsManager(GraphicsDevice device, ContentManager cmg)
			: base(device, cmg)
		{
			// lighting
			Ambient = new Color(64, 64, 64);
			OuterBackgroundColor = new Vector3(0.25f, 0.25f, 0.25f);
			InnerBackgroundColor = new Vector3(-1, 0, 0);
			EnableFog = false;
			FogBegin = 0;
			FogEnd = 1;
			dynamicLights = new List<Light>();
			sceneLights = new List<Light>();
			waterfallTimer = 0;
		}

		/// <summary>
		/// Loads shader content into graphics memory and sets up
		/// render targets.
		/// </summary>
        protected override void LoadEffects()
        {
			LoadRenderTargets();

			// Load shaders
			deferredEffect = Content.Load<Effect>(@"Shaders\Deferred");
            deferredSkinnedEffect = Content.Load<Effect>(@"Shaders\DeferredSkinned");
			deferredTextureEffect = Content.Load<Effect>(@"Shaders\DeferredTextured");
            portraitEffect = Content.Load<Effect>(@"Shaders\PortraitPostProcess");
			forwardUnshadedEffect = Content.Load<Effect>(@"Shaders\ForwardUnshaded");
			instanceEffect = Content.Load<Effect>(@"Shaders\DeferredInstanced");

			clearEffect = Content.Load<Effect>(@"Shaders\ClearMRT");
			edgeEffect = Content.Load<Effect>(@"Shaders\EdgeHighlight");
			ambientLightEffect = content.Load<Effect>(@"Shaders\AmbientLight");
			pointLightEffect = Content.Load<Effect>(@"Shaders\PointLight");
			directionalLightEffect = Content.Load<Effect>(@"Shaders\DirectionalLight");
			shieldEffect = Content.Load<Effect>(@"Shaders\Shield");
			fogEffect = content.Load<Effect>(@"Shaders\Fog");

			hdrEffect = Content.Load<Effect>(@"Shaders\HDR");
        }

		/// <summary>
		/// Helper function for loading deferred render targets.
		/// </summary>
		private void LoadRenderTargets()
		{
			deferredTargets = new RenderTargetBinding[nRenderTargets];

			// Albedo + Emissive
			deferredTargets[0] = new RenderTargetBinding(
				new RenderTarget2D(
					Device,
					Device.PresentationParameters.BackBufferWidth,
					Device.PresentationParameters.BackBufferHeight,
					false,
					SurfaceFormat.Rgba1010102,
					DepthFormat.Depth24));
			// Depth
			deferredTargets[1] = new RenderTargetBinding(
				new RenderTarget2D(
					Device,
					Device.PresentationParameters.BackBufferWidth,
					Device.PresentationParameters.BackBufferHeight,
					false,
					SurfaceFormat.Single,
					DepthFormat.Depth24));
			// Normal
			deferredTargets[2] = new RenderTargetBinding(
				new RenderTarget2D(
					Device,
					Device.PresentationParameters.BackBufferWidth,
					Device.PresentationParameters.BackBufferHeight,
					false,
					SurfaceFormat.HalfVector2,
					DepthFormat.Depth24));

			// Shadow/light map
			lightMap = new RenderTarget2D(Device, 1024, 1024, false, SurfaceFormat.Alpha8, DepthFormat.Depth24);

			// Light accumulation buffer (stage before post-process
			lightAccumulationBuffer = new RenderTarget2D(
				Device,
				Device.PresentationParameters.BackBufferWidth,
				Device.PresentationParameters.BackBufferHeight,
				false,
				SurfaceFormat.Rgba1010102,
				DepthFormat.None,
				0,
				RenderTargetUsage.PreserveContents);

			bloomAccumulationBuffer = new RenderTarget2D(
				Device,
				Device.PresentationParameters.BackBufferWidth / 4,
				Device.PresentationParameters.BackBufferHeight / 4,
				false,
				SurfaceFormat.Rgba1010102,
				DepthFormat.None,
				0,
				RenderTargetUsage.PreserveContents);

			bloomDownSampleBuffers = new RenderTarget2D[3];
			float downSampleAmount = 4;
			for (int i = 0; i < 3; i++)
			{
				bloomDownSampleBuffers[i] = new RenderTarget2D(
					Device,
					(int)(Device.PresentationParameters.BackBufferWidth / downSampleAmount),
					(int)(Device.PresentationParameters.BackBufferHeight / downSampleAmount),
					false,
					SurfaceFormat.Rgba1010102,
					DepthFormat.None,
					0,
					RenderTargetUsage.PreserveContents);
				downSampleAmount *= 4;
			}
		}

		/// <summary>
		/// Loads a model from a file and places it in the models dictionary.
		/// </summary>
		/// <param name="fileName">Path to model</param>
		/// <param name="texture">Texture override (null for default)</param>
		protected override GModel LoadModel(string fileName, string texture)
		{
			return LoadModel(fileName, texture, null);
		}

		/// <summary>
		/// Loads a model from a file and places it in the models dictionary.
		/// </summary>
		/// <param name="fileName">Path to model</param>
		/// <param name="texture">Texture override (null for default)</param>
		/// <param name="emissiveTexture">Emissive texture (or null)</param>
		protected override GModel LoadModel(string fileName, string texture, string emissiveTexture)
		{
			GModel gmodel;

			// Check for pre-existence
			List<GModel> modelList;
			if (models.ContainsKey(fileName))
			{
				modelList = models[fileName];

				// Find GModel if it exists
				foreach (GModel listModel in modelList)
				{
					if (listModel.TexturePath == texture && listModel.EmissivePath == emissiveTexture)
					{
						return listModel;
					}
				}

				// Didn't find, copy an existing model
				gmodel = new GModel(modelList[0]);
			}
			else
			{
				gmodel = new GModel();
				modelList = new List<GModel>();
				modelList.Add(gmodel);
				models.Add(fileName, modelList);

				// Load model
				gmodel.model = content.Load<Model>(fileName);
				gmodel.Texture = null;

				//Load skinning and animation (null if none)
				gmodel.skinningData = gmodel.model.Tag as SkinningData;
			}

			// Assign texture if one is provided
			if (texture != null)
			{
				gmodel.Texture = content.Load<Texture2D>(texture);
				gmodel.MaterialColors = null;
			}

			// Else, check for materials and textures
			LinkedList<Vector3> colors = new LinkedList<Vector3>();
			foreach (ModelMesh mesh in gmodel.model.Meshes)
			{
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					BasicEffect be = part.Effect as BasicEffect;
					if (be != null && be.TextureEnabled)
					{
						gmodel.Texture = be.Texture; // only one texture supported
					}
					else
					{
						colors.AddLast(be != null ? be.DiffuseColor : Vector3.One);
					}
				}
			}

			gmodel.MaterialColors = gmodel.Texture == null ? colors.ToArray() : null;
			gmodel.EmissiveTexture = null;
			if (emissiveTexture != null)
				gmodel.EmissiveTexture = content.Load<Texture2D>(emissiveTexture);
			gmodel.Emissive = 0;

			// HACK: workaround for the lack of a materials system
			gmodel.IsWaterfall = (fileName == @"MapObjects\RttT\models\waterfall-1");

			return gmodel;
		}

		/// <summary>
		/// Called in the editor when the view is reset.
		/// </summary>
		public override void Reset(object sender, EventArgs args)
		{
			// Resize render targets
			deferredTargets[0].RenderTarget.Dispose();
			deferredTargets[1].RenderTarget.Dispose();
			deferredTargets[2].RenderTarget.Dispose();

			lightMap.Dispose();
			lightAccumulationBuffer.Dispose();

			LoadRenderTargets();
		}

		#endregion

		#region Lighting

		/// <summary>
		/// Adds a dynamic light to the scene. Will be rendered every
		/// frame until RemoveLight is called.
		/// </summary>
		/// <param name="light">Light</param>
		public override void AddDynamicLight(Light light)
		{
			if (light.Type == LightType.Directional)
				throw new ArgumentException("Dynamic lights can't be directional");
			dynamicLights.Add(light);
		}

		/// <summary>
		/// Adds a dynamic light to the scene. Will be rendered every frame
		/// until RemoveLight is called. Has priority over normal lights.
		/// </summary>
		/// <param name="light"></param>
		public override void AddSceneLight(Light light)
		{
			sceneLights.Add(light);
		}

		/// <summary>
		/// Removes a light from the scene.
		/// </summary>
		/// <param name="light">Light</param>
		public override void RemoveLight(Light light)
		{
			dynamicLights.Remove(light);
			sceneLights.Remove(light);
		}

		public override void RemoveAllDynamicLights()
		{
			dynamicLights.Clear();
		}

		/// <summary>
		/// Clears all lights from the scene.
		/// </summary>
		public override void RemoveAllLights()
		{
			dynamicLights.Clear();
			sceneLights.Clear();
			Ambient = new Color(64, 64, 64);
		}

		/// <summary>
		/// Returns the directional light that is casting shadows, or null.
		/// </summary>
		public override Light ShadowCastingLight
		{
			get
			{
				if (sceneLights.Count == 0)
					return null;
				else if (sceneLights[0].Type == LightType.Point)
					return null;
				return sceneLights[0];
			}
		}

		#endregion

		#region Drawing

		/// <summary>
        /// Called at the beginning of the drawing phase; starts deferred
		/// rendering stage.
        /// </summary>
        /// <param name="gameTime">Current game time</param>
		public override void StartDrawing(GameTime gameTime, CameraManager camera)
        {
			if (renderState != RenderState.None)
				throw new InvalidOperationException("renderstate is " + renderState.ToString());
			renderState = RenderState.Deferred;

			this.gameTime = gameTime;
			this.camera = camera;

			Device.DepthStencilState = DepthStencilState.Default;
			Device.BlendState = BlendState.Opaque;

			Device.SetRenderTargets(deferredTargets);
			Device.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1, 0);
			ClearRenderTargets();

			// set effect parameters
			deferredEffect.Parameters["xViewProjection"].SetValue(camera.View * camera.Projection);
            deferredSkinnedEffect.Parameters["xViewProjection"].SetValue(camera.View * camera.Projection);
			deferredTextureEffect.Parameters["xViewProjection"].SetValue(camera.View * camera.Projection);
			instanceEffect.Parameters["xViewProjection"].SetValue(camera.View * camera.Projection);

			// update waterfall timer
			waterfallTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
			while (waterfallTimer >= waterfallPeriod)
				waterfallTimer -= waterfallPeriod;

			nDrawCalls = 0;
        }

        /// <summary>
        /// used for rendering shadows
        /// </summary>
        public override void StopDeferredStartShadow()
		{
			if (renderState != RenderState.Deferred)
				throw new InvalidOperationException("renderstate is " + renderState.ToString());
			renderState = RenderState.Shadow;

			Device.SetRenderTarget(lightMap);

            Device.DepthStencilState = DepthStencilState.Default;
            Device.BlendState = BlendState.Opaque;
			Device.RasterizerState = RasterizerState.CullNone;

			// Calculate light camera matrix
			Light light = ShadowCastingLight;
			if (light != null)
			{
				// Light camera algorithm:
					// Take normal camera information, rotate 
				const int kLightDistance = 200;
				lightViewProjection = 
					Matrix.CreateLookAt(Vector3.Normalize(light.Location) * kLightDistance, Vector3.Zero, Vector3.UnitY)
				  * Matrix.CreateOrthographic(150, 150, 10, 500);
			}

			Device.Clear(ClearOptions.Target, Color.Black, 1, 0);

			deferredEffect.Parameters["xViewProjection"].SetValue(lightViewProjection);
			instanceEffect.Parameters["xViewProjection"].SetValue(lightViewProjection);
        }

		/// <summary>
		/// Stop the drawing sequence. Applies any post effects and processing,
		/// then resets the GraphicsManager state for the next frame.
		/// </summary>
		public override void StopShadowStartLighting()
		{
			if (renderState != RenderState.Shadow)
				throw new InvalidOperationException("renderstate is " + renderState.ToString());
			renderState = RenderState.Lighting;

			Device.SetRenderTarget(lightAccumulationBuffer);

			// Post-process and render deferred targets back to screen.
			Device.Clear(Color.Black);
			Device.DepthStencilState = DepthStencilState.None;

			// Fix device parameters
			RasterizerState rs = new RasterizerState();
			rs.FillMode = FillMode.Solid;
			rs.CullMode = CullMode.None;
			Device.RasterizerState = rs;
			Device.DepthStencilState = DepthStencilState.DepthRead;

			RenderLightContributions();
			RenderEdges();
			RenderFogAndBackground();

			// Set effect parameters
			shieldEffect.Parameters["xViewProjection"].SetValue(camera.View * camera.Projection);
			shieldEffect.Parameters["xView"].SetValue(camera.View);
			shieldEffect.Parameters["xDepthTex"].SetValue(deferredTargets[1].RenderTarget);
		}

        /// <summary>
        /// Used for rendering characters on menu screens.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="camera"></param>
        public override void StartDrawingCharacters(GameTime gameTime, CameraManager camera, RenderTarget2D rt, Viewport viewport)
		{
			if (renderState != RenderState.None)
				throw new InvalidOperationException("renderstate is " + renderState.ToString());
			renderState = RenderState.Deferred;

            this.gameTime = gameTime;
            this.camera = camera;
            Device.DepthStencilState = DepthStencilState.Default;
            Device.BlendState = BlendState.Opaque;

            // Clear depth target
            Device.SetRenderTargets(deferredTargets);
            Device.Clear(Color.White);
            Device.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1, 0);

            Device.BlendState = BlendState.Opaque;
            Device.DepthStencilState = DepthStencilState.Default;
            Device.SamplerStates[0] = SamplerState.LinearWrap;

            Matrix viewProjection = camera.View * camera.Projection;
            deferredEffect.Parameters["xViewProjection"].SetValue(viewProjection);
            deferredSkinnedEffect.Parameters["xViewProjection"].SetValue(viewProjection);
            deferredTextureEffect.Parameters["xViewProjection"].SetValue(viewProjection);
			deferredEffect.CurrentTechnique = deferredEffect.Techniques["Deferred"];
            deferredSkinnedEffect.CurrentTechnique = deferredSkinnedEffect.Techniques["DeferredSkinned"];
			deferredTextureEffect.CurrentTechnique = deferredTextureEffect.Techniques["DeferredTextured"];

            Device.Viewport = viewport;
        }

		/// <summary>
		/// Clears deferred MRTs
		/// </summary>
		private void ClearRenderTargets()
		{
			// Fix device parameters
			RasterizerState rs = new RasterizerState();
			rs.FillMode = FillMode.Solid;
			rs.CullMode = CullMode.None;
			Device.RasterizerState = rs;
			Device.DepthStencilState = DepthStencilState.DepthRead;
			RenderEffectToScreen(clearEffect);
			Device.DepthStencilState = DepthStencilState.Default;
		}

		/// <summary>
		/// Applies lighting contributions from all lights.
		/// </summary>
		private void RenderLightContributions()
		{
			// MUST HAVE ADDITIVE BLENDING
			Device.BlendState = BlendState.Additive;

			// Calculate inverse view-projection matrix (used for point lights
			Matrix viewProjection = camera.View * camera.Projection;
			Matrix inverseViewProjection;
			Matrix.Invert(ref viewProjection, out inverseViewProjection);
			pointLightEffect.Parameters["xInverseViewProjection"].SetValue(inverseViewProjection);
			directionalLightEffect.Parameters["xInverseViewProjection"].SetValue(inverseViewProjection);

			// Set static shader parameters
			directionalLightEffect.Parameters["xAlbedoTex"].SetValue(deferredTargets[0].RenderTarget);
			directionalLightEffect.Parameters["xDepthTex"].SetValue(deferredTargets[1].RenderTarget);
			directionalLightEffect.Parameters["xNormalTex"].SetValue(deferredTargets[2].RenderTarget);
			directionalLightEffect.Parameters["xLightTex"].SetValue(lightMap);
			pointLightEffect.Parameters["xAlbedoTex"].SetValue(deferredTargets[0].RenderTarget);
			pointLightEffect.Parameters["xDepthTex"].SetValue(deferredTargets[1].RenderTarget);
			pointLightEffect.Parameters["xNormalTex"].SetValue(deferredTargets[2].RenderTarget);

			// Render all the lights
			RenderAmbientLight();

			bool firstLight = true;
			foreach (Light light in sceneLights)
			{
				switch (light.Type)
				{
					case LightType.Directional:
						RenderDirectionalLight(light, firstLight);
						firstLight = false;
						break;
					case LightType.Point:
						RenderPointLight(light, false);
						break;
				}
			}
			firstLight = false;

			// Render all the lights
			foreach (Light light in dynamicLights)
			{
				switch (light.Type)
				{
					case LightType.Directional:
						RenderDirectionalLight(light, false);
						break;
					case LightType.Point:
						RenderPointLight(light, false);
						break;
				}
			}
		}

		/// <summary>
		/// Applies contribution from ambient light.
		/// </summary>
		private void RenderAmbientLight()
		{
			// Parameters
			ambientLightEffect.Parameters["xAlbedoTex"].SetValue(deferredTargets[0].RenderTarget);
			ambientLightEffect.Parameters["xDepthTex"].SetValue(deferredTargets[1].RenderTarget);
			ambientLightEffect.Parameters["xAmbient"].SetValue(Ambient.ToVector3());

			float inverseAspect = Device.PresentationParameters.BackBufferHeight
								/ Device.PresentationParameters.BackBufferWidth;
			ambientLightEffect.Parameters["xInverseAspect"].SetValue(inverseAspect);

			RenderEffectToScreen(ambientLightEffect);
		}

		/// <summary>
		/// Applies contribution from a point light.
		/// </summary>
		/// <param name="light">Light</param>
		private void RenderPointLight(Light light, bool shadow)
		{
			pointLightEffect.CurrentTechnique = pointLightEffect.Techniques["PointLight"];
			//pointLightEffect.Parameters["xLightTex"].SetValue(lightMap);
			//pointLightEffect.Parameters["xLightViewProjection"].SetValue(cameraViewProjection);
			pointLightEffect.Parameters["xLightColorAndIntensity"].SetValue(light.Color.ToVector3() * light.Intensity);
			pointLightEffect.Parameters["xLightPosition"].SetValue(light.Location);

			// TODO: light volume shapes

			RenderEffectToScreen(pointLightEffect);
		}

		/// <summary>
		/// Applies contribution from a directional light.
		/// </summary>
		/// <param name="light">Light</param>
		private void RenderDirectionalLight(Light light, bool shadow)
		{
			if (light.Intensity != 0)
			{
				directionalLightEffect.CurrentTechnique = shadow
					? directionalLightEffect.Techniques["DirectionalLightShadow"]
					: directionalLightEffect.Techniques["DirectionalLight"];
				directionalLightEffect.Parameters["xLightViewProjection"].SetValue(lightViewProjection);
				directionalLightEffect.Parameters["xLightColorAndIntensity"].SetValue(light.Color.ToVector3() * light.Intensity);
				directionalLightEffect.Parameters["xLightNormal"].SetValue(Vector3.Normalize(light.Location));

				RenderEffectToScreen(directionalLightEffect);
			}
		}

		/// <summary>
		/// Renders background color and applies fog effect (if enabled).
		/// </summary>
		private void RenderFogAndBackground()
		{
			// Fog and background
			if (EnableFog)
			{
				fogEffect.CurrentTechnique = fogEffect.Techniques["Fog"];

				Matrix inverseViewProjection;
				Matrix.Invert(ref camera.ViewProjection, out inverseViewProjection);

				fogEffect.Parameters["xInverseViewProjection"].SetValue(inverseViewProjection);
				fogEffect.Parameters["xFogBegin"].SetValue(FogBegin);
				fogEffect.Parameters["xFogEnd"].SetValue(FogEnd);
			}
			else
			{
				fogEffect.CurrentTechnique = fogEffect.Techniques["NoFog"];
			}
			fogEffect.Parameters["xDepthTex"].SetValue(deferredTargets[1].RenderTarget);
			fogEffect.Parameters["xOuterColor"].SetValue(OuterBackgroundColor / 4);
			fogEffect.Parameters["xCenterColor"].SetValue(
				InnerBackgroundColor.X >= 0 ? InnerBackgroundColor / 4 : OuterBackgroundColor / 4);

			Device.BlendState = BlendState.NonPremultiplied;
			RenderEffectToScreen(fogEffect);
		}

		/// <summary>
		/// Renders ambient lighting contribution and edge highlights.
		/// </summary>
		private void RenderEdges()
		{
			Device.BlendState = BlendState.NonPremultiplied;

			// Inputs
			edgeEffect.Parameters["xDepthTex"].SetValue(deferredTargets[1].RenderTarget);
			edgeEffect.Parameters["xNormalTex"].SetValue(deferredTargets[2].RenderTarget);
			edgeEffect.Parameters["xResolution"].SetValue(new Vector2(
				Device.PresentationParameters.BackBufferWidth,
				Device.PresentationParameters.BackBufferHeight));

			// Settings
#if GRAPHICS_DEBUG
			edgeEffect.Parameters["xEdgeWidth"].SetValue(GraphicsOptionsForm.EdgeWidth);
			edgeEffect.Parameters["xEdgeIntensity"].SetValue(GraphicsOptionsForm.EdgeIntensity);
			edgeEffect.Parameters["xNormalSensitivity"].SetValue(GraphicsOptionsForm.NormalSensitivity);
			edgeEffect.Parameters["xDepthSensitivity"].SetValue(GraphicsOptionsForm.DepthSensitivity);
			edgeEffect.Parameters["xNormalThreshold"].SetValue(GraphicsOptionsForm.NormalThreshold);
			edgeEffect.Parameters["xDepthThreshold"].SetValue(GraphicsOptionsForm.DepthThreshold);
#else
			edgeEffect.Parameters["xEdgeWidth"].SetValue(0.5f);
			edgeEffect.Parameters["xEdgeIntensity"].SetValue(1);
			edgeEffect.Parameters["xNormalSensitivity"].SetValue(1);
			edgeEffect.Parameters["xDepthSensitivity"].SetValue(1);
			edgeEffect.Parameters["xNormalThreshold"].SetValue(0.6f);
			edgeEffect.Parameters["xDepthThreshold"].SetValue(0.1f);
#endif
			RenderEffectToScreen(edgeEffect);
		}

		/// <summary>
		/// Renders a full-screen effect.
		/// </summary>
		/// <param name="effect">Full-screen effect</param>
		private void RenderEffectToScreen(Effect effect)
		{
			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				VertexPositionTexture[] vertices = 
				{
					// Triangle 1
					new VertexPositionTexture(new Vector3(-1,-1, 0), new Vector2(0, 1)), // bot-l
					new VertexPositionTexture(new Vector3( 1,-1, 0), new Vector2(1, 1)), // bot-r
					new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)), // top-l
					// Triangle 2
					new VertexPositionTexture(new Vector3( 1, 1, 0), new Vector2(1, 0)), // top-r
				};

				Device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, 2);
			}
		}

        /// <summary>
        /// Finishes drawing a portrait by rendering temporary data into a render target.
        /// </summary>
        /// <param name="rt"></param>
		public override void FinalizePortrait(RenderTarget2D rt)
        {
			if (renderState != RenderState.Deferred)
				throw new InvalidOperationException("renderstate is " + renderState.ToString());
			renderState = RenderState.None;

            Device.SetRenderTarget(rt);
            Device.Clear(Color.Transparent);

            // Fix device parameters
            RasterizerState rs = new RasterizerState();
            rs.FillMode = FillMode.Solid;
            rs.CullMode = CullMode.None;
            Device.RasterizerState = rs;
            Device.DepthStencilState = DepthStencilState.None;
			Device.BlendState = BlendState.NonPremultiplied;

            // effect parameters
            float texWidth = (float)Device.Viewport.Width / Device.PresentationParameters.BackBufferWidth;
            float texHeight = (float)Device.Viewport.Height / Device.PresentationParameters.BackBufferHeight;

            Matrix inverseViewProjection = Matrix.Invert(camera.View * camera.Projection);
            portraitEffect.Parameters["xInverseViewProjection"].SetValue(inverseViewProjection);

            // Source textures
            portraitEffect.Parameters["xAlbedoTex"].SetValue(deferredTargets[0].RenderTarget);
            portraitEffect.Parameters["xDepthTex"].SetValue(deferredTargets[1].RenderTarget);
            portraitEffect.Parameters["xNormalTex"].SetValue(deferredTargets[2].RenderTarget);

            // Draw
            foreach (EffectPass pass in portraitEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                VertexPositionTexture[] vertices = 
				{
					// Triangle 1
					new VertexPositionTexture(new Vector3(-1,-1, 0), new Vector2(0, texHeight)), // bot-l
					new VertexPositionTexture(new Vector3( 1,-1, 0), new Vector2(texWidth, texHeight)), // bot-r
					new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)), // top-l
					// Triangle 2
					new VertexPositionTexture(new Vector3( 1, 1, 0), new Vector2(texWidth, 0)), // top-r
				};

                Device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, 2);
            }

            // Default viewport
            Device.SetRenderTarget(null);
#if GAME
			IndependentResolutionRendering.Resolution.ResetViewport();
#endif
        }

		public override void StopDrawing()
		{
			if (renderState != RenderState.Lighting)
				throw new InvalidOperationException("renderstate is " + renderState.ToString());
			renderState = RenderState.None;

			// From now on, we're only rendering full-screen quads
			Device.DepthStencilState = DepthStencilState.None;
			Device.RasterizerState = RasterizerState.CullNone;

			// HDR/bloom post-process

			Device.BlendState = BlendState.Opaque;

			// Downsample
			Vector2 pixelOffset = new Vector2(
				1.0f / bloomDownSampleBuffers[0].Width,
				1.0f / bloomDownSampleBuffers[0].Height);

			hdrEffect.CurrentTechnique = hdrEffect.Techniques["DownsampleCutoff"]; // offset color values into HDR range

			Device.SetRenderTarget(bloomDownSampleBuffers[0]);
			hdrEffect.Parameters["xSceneTex"].SetValue(lightAccumulationBuffer);
			hdrEffect.Parameters["xOffset"].SetValue(pixelOffset);
			RenderEffectToScreen(hdrEffect);

			hdrEffect.CurrentTechnique = hdrEffect.Techniques["Downsample"]; // do not perform color value offsets

			Device.SetRenderTarget(bloomDownSampleBuffers[1]);
			hdrEffect.Parameters["xSceneTex"].SetValue(bloomDownSampleBuffers[0]);
			hdrEffect.Parameters["xOffset"].SetValue(pixelOffset * 4);
			RenderEffectToScreen(hdrEffect);

			Device.SetRenderTarget(bloomDownSampleBuffers[2]);
			hdrEffect.Parameters["xSceneTex"].SetValue(bloomDownSampleBuffers[1]);
			hdrEffect.Parameters["xOffset"].SetValue(pixelOffset * 16);
			RenderEffectToScreen(hdrEffect);

			// Render tone-mapped light accumulation buffer to screen

			// Upward blur and accumulate downsampled textures
			BlurAndAccumulate(bloomDownSampleBuffers[2], bloomDownSampleBuffers[1], pixelOffset * 16);
			BlurAndAccumulate(bloomDownSampleBuffers[1], bloomDownSampleBuffers[0], pixelOffset * 4);
			//Device.SetRenderTarget(lightAccumulationBuffer); Device.Clear(Color.Black);
			BlurAndAccumulate(bloomDownSampleBuffers[0], lightAccumulationBuffer, pixelOffset); // final pass applies to main screen

			Device.SetRenderTarget(null);
			Device.BlendState = BlendState.Opaque;
			hdrEffect.CurrentTechnique = hdrEffect.Techniques["Tonemap"];
			hdrEffect.Parameters["xSceneTex"].SetValue(lightAccumulationBuffer);
			RenderEffectToScreen(hdrEffect);

			// Fix viewport
				// todo...

			// Set device textures to null. Fixes Editor resize bug.
			Device.Textures[0] = null;
			Device.Textures[1] = null;
			Device.Textures[2] = null;

#if GAME
			IndependentResolutionRendering.Resolution.ResetViewport();
#endif
		}

		private void BlurAndAccumulate(RenderTarget2D source, RenderTarget2D destination, Vector2 resolution)
		{
			Device.BlendState = BlendState.Opaque;

			// vertical(source) -> bloomAccumulationBuffer

			Device.SetRenderTarget(bloomAccumulationBuffer);
			hdrEffect.Parameters["xSceneTex"].SetValue(source);
			hdrEffect.CurrentTechnique = hdrEffect.Techniques["BlurVertical"];

			hdrEffect.Parameters["xOffset"].SetValue(resolution);
			hdrEffect.Parameters["xUvOffset"].SetValue(Vector2.One);
			RenderEffectToScreen(hdrEffect);

			// horizontal(bloomAccumulationBuffer) -> destination

			Device.SetRenderTarget(destination);
			Device.BlendState = (destination != null ? BlendState.Additive : BlendState.Opaque);
			hdrEffect.Parameters["xSceneTex"].SetValue(bloomAccumulationBuffer);
			hdrEffect.CurrentTechnique = hdrEffect.Techniques["BlurHorizontal"];

			RenderEffectToScreen(hdrEffect);
		}

		/// <summary>
		/// Draws a model with deferred shading.
		/// </summary>
		/// <param name="model">Model</param>
		/// <param name="position">Position</param>
		/// <param name="rotation">Model rotation</param>
		public override void DrawModel(GModel gmodel, Vector3 position, Quaternion rotation, float scale)
		{
            DrawModel(gmodel, position, rotation, new Vector3(scale), null);
        }

		/// <summary>
		/// Draws a model with deferred shading.
		/// </summary>
		/// <param name="gmodel">Model info</param>
		/// <param name="position">Position</param>
		/// <param name="rotation">Rotation</param>
		/// <param name="scale">Scale</param>
		/// <param name="bones">Animation data.</param>
        public override void DrawModel(GModel gmodel, Vector3 position, Quaternion rotation, float scale, Matrix[] bones)
        {
            DrawModel(gmodel, position, rotation, new Vector3(scale), bones);
        }

        /// <summary>
        /// Draws a model with deferred shading.
        /// </summary>
		/// <param name="gmodel">Model info</param>
		/// <param name="position">Position</param>
		/// <param name="rotation">Rotation</param>
		/// <param name="scale">Scale</param>
		/// <param name="bones">Animation data.</param>
		public override void DrawModel(GModel gmodel, Vector3 position, Quaternion rotation, Vector3 scale, Matrix[] bones)
        {
            // Rasterizer state settings
            if (renderState == RenderState.Deferred && Device.RasterizerState.FillMode != FillMode.Solid)
            {
                RasterizerState rs = new RasterizerState();
                rs.FillMode = FillMode.Solid;
				rs.CullMode = CullMode.CullCounterClockwiseFace;
                Device.RasterizerState = rs;
            }

            DrawDeferredShadedModel(gmodel, ref position, ref rotation, ref scale, true, bones);
        }

		/// <summary>
		/// Uses geometry instancing to draw several models. Models must have one
		/// ModelMesh with a single ModelMeshPart.
		/// </summary>
		/// <param name="model">Model</param>
		/// <param name="transforms">Transform data</param>
		/// <param name="nPlatforms">Number of transforms</param>
		public override void DrawInstancedPlatforms(GModel gmodel, Matrix[] transforms, int nPlatforms)
		{
			// The single mesh part being drawn.
			ModelMeshPart meshPart = gmodel.model.Meshes[0].MeshParts[0];

			if (instanceBuffer == null || instanceBuffer.VertexCount < nPlatforms)
			{
				if (instanceBuffer != null)
					instanceBuffer.Dispose();

				instanceBuffer = new DynamicVertexBuffer(Device, instanceDeclaration,
														 nPlatforms, BufferUsage.WriteOnly);
			}

			// Draw model
			Device.RasterizerState = RasterizerState.CullCounterClockwise;

			// Upload instance transform data
			Device.SetVertexBuffers();
			instanceBuffer.SetData(transforms, 0, nPlatforms, SetDataOptions.Discard);
			Device.SetVertexBuffers(
				new VertexBufferBinding(meshPart.VertexBuffer),
				new VertexBufferBinding(instanceBuffer, 0, 1)
			);
			Device.Indices = meshPart.IndexBuffer;

			if (gmodel.Texture != null && renderState == RenderState.Deferred)
			{
				instanceEffect.CurrentTechnique = instanceEffect.Techniques["DeferredInstancedTextured"];
				instanceEffect.Parameters["xTexture"].SetValue(gmodel.Texture);
			}
			else if (renderState == RenderState.Deferred)
			{
				instanceEffect.CurrentTechnique = instanceEffect.Techniques["DeferredInstanced"];
				instanceEffect.Parameters["xColor"].SetValue(gmodel.MaterialColors[0]);
			}
			else
			{
				instanceEffect.CurrentTechnique = instanceEffect.Techniques["DeferredInstancedLight"];
			}

			// Emissive
			if (gmodel.EmissiveTexture != null)
			{
				instanceEffect.Parameters["xEnableEmissiveTexture"].SetValue(1);
				instanceEffect.Parameters["xEmissiveTex"].SetValue(gmodel.EmissiveTexture);
				instanceEffect.Parameters["xEmissive"].SetValue(gmodel.Emissive);
			}
			else
			{
				instanceEffect.Parameters["xEnableEmissiveTexture"].SetValue(0);
				instanceEffect.Parameters["xEmissive"].SetValue(gmodel.Emissive);
			}

			instanceEffect.Parameters["xTextureOffset"].SetValue(
				gmodel.IsWaterfall ? waterfallTimer / waterfallPeriod : 0);

			instanceEffect.CurrentTechnique.Passes[0].Apply();

			Device.DrawInstancedPrimitives(
				PrimitiveType.TriangleList, 0, 0,
				meshPart.NumVertices, meshPart.StartIndex,
				meshPart.PrimitiveCount, nPlatforms);
		}

		/// <summary>
		/// Draws a character's shield.
		/// </summary>
		/// <param name="shieldModel">Model</param>
		/// <param name="position">Center position</param>
		/// <param name="scale">Scale</param>
		public override void DrawShield(GModel shieldModel, Vector3 position, float scale)
		{
			// Rasterizer state settings
			if (Device.RasterizerState.FillMode != FillMode.Solid)
			{
				RasterizerState rs = new RasterizerState();
				rs.FillMode = FillMode.Solid;
				rs.CullMode = CullMode.CullCounterClockwiseFace;
				Device.RasterizerState = rs;
			}

			Device.BlendState = BlendState.Additive;

			// Parameters
			Matrix world = Matrix.CreateScale(scale) * Matrix.CreateTranslation(position);
			shieldEffect.Parameters["xWorld"].SetValue(world);
			shieldEffect.Parameters["xShieldTex"].SetValue(shieldModel.Texture);

			// Draw
			shieldEffect.CurrentTechnique.Passes[0].Apply(); // single-pass
			foreach (ModelMeshPart part in shieldModel.model.Meshes[0].MeshParts)
			{
				Device.Indices = part.IndexBuffer;
				Device.SetVertexBuffer(part.VertexBuffer);
				Device.DrawIndexedPrimitives(
					PrimitiveType.TriangleList,
					part.VertexOffset,
					0,
					part.NumVertices,
					part.StartIndex,
					part.PrimitiveCount);

				// draw call count
				nDrawCalls++;
			}
		}

		/// <summary>
		/// Draws a bounding box.
		/// </summary>
		/// <param name="bb">Bounding Box</param>
		/// <param name="position">Center position of the bounding box</param>
		/// <param name="rotation">Box rotation</param>
		public override void DrawBoundingBox(BoundingBox bb, Vector3 position)
		{
#if GAME
            if (Game1.debug_boundingboxes)
#endif
            {
                // Enable wireframe, disable culling
                if (Device.RasterizerState.FillMode != FillMode.WireFrame)
                {
                    RasterizerState rs = new RasterizerState();
                    rs.FillMode = FillMode.WireFrame;
                    rs.CullMode = CullMode.None;
                    Device.RasterizerState = rs;

					BlendState bs = BlendState.Opaque;
                }
                GModel model = models["Testing\\boundingbox"][0];
                Quaternion rotation = Quaternion.Identity;
                Vector3 center = (bb.Max + bb.Min) / 2;
                Vector3 size = (bb.Max - bb.Min) / 2;
                // Draw box
                DrawForwardShadedModel(
                    model,
                    ref center, // center of the BB
                    ref rotation,
                    ref size, // size of the BB
                    false);
            }
		}

        /// <summary>
        /// Draws a wireframe sphere.
        /// </summary>
        /// <param name="radius">Sphere radius</param>
        /// <param name="position">Sphere position</param>
		public override void DrawBoundingSphere(float radius, Vector3 position)
        {
            //if (Game1.debug_boundingboxes)
            {
                // Enable wireframe, disable culling
                if (Device.RasterizerState.FillMode != FillMode.WireFrame)
                {
                    RasterizerState rs = new RasterizerState();
                    rs.FillMode = FillMode.WireFrame;
                    rs.CullMode = CullMode.None;
                    Device.RasterizerState = rs;

					BlendState bs = new BlendState();
					bs.ColorSourceBlend = Blend.SourceAlpha;
					bs.AlphaSourceBlend = Blend.SourceAlpha;
					bs.ColorDestinationBlend = Blend.InverseSourceAlpha;
					bs.AlphaDestinationBlend = Blend.InverseSourceAlpha;
					Device.BlendState = bs;
                }
                GModel model =  models["Testing\\icosphere"][0];
                Quaternion rotation = Quaternion.Identity;
                Vector3 scale = new Vector3(radius);
                // Draw box
                DrawForwardShadedModel(
					model,
                    ref position, // center of the BB
                    ref rotation,
                    ref scale,
                    false);
            }
        }

		/// <summary>
		/// Draw forward-rendered model.
		/// </summary>
		/// <param name="model">Model to draw</param>
		/// <param name="position">Position</param>
		/// <param name="rotation">Rotation</param>
		/// <param name="scale">Scale (x, y, z)</param>
		/// <param name="enableShading">Enables textures and lighting</param>
		private void DrawForwardShadedModel(
			GModel gmodel,
			ref Vector3 position,
			ref Quaternion rotation,
			ref Vector3 scale,
			bool enableShading)
		{
			Model model = gmodel.model;

			// Calculate transforms
			Matrix world = Matrix.CreateScale(scale)
							* Matrix.CreateFromQuaternion(rotation)
							* Matrix.CreateTranslation(position);
			Matrix[] meshTransforms = new Matrix[model.Bones.Count];
			model.CopyAbsoluteBoneTransformsTo(meshTransforms); // get bone transforms

            // Render the mesh
            foreach (ModelMesh mesh in model.Meshes)
            {
				// Set effect data
				Matrix animatedWorld = meshTransforms[mesh.ParentBone.Index] * world;
				Matrix worldViewProjection = animatedWorld * camera.View * camera.Projection;
				
				forwardUnshadedEffect.Parameters["xDepthTex"].SetValue(deferredTargets[1].RenderTarget);
				forwardUnshadedEffect.Parameters["xWorld"].SetValue(animatedWorld);
				forwardUnshadedEffect.Parameters["xWorldViewProjection"].SetValue(worldViewProjection);

				// Draw
				forwardUnshadedEffect.CurrentTechnique.Passes[0].Apply(); // single-pass
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					Device.Indices = part.IndexBuffer;
					Device.SetVertexBuffer(part.VertexBuffer);
					Device.DrawIndexedPrimitives(
						PrimitiveType.TriangleList,
						part.VertexOffset,
						0,
						part.NumVertices,
						part.StartIndex,
						part.PrimitiveCount);

					// draw call count
					nDrawCalls++;
				}
            }
        }

		/// <summary>
		/// Draws models, shaded or unshaded. Contains the actual drawing code.
		/// </summary>
		/// <param name="model">Model to draw</param>
		/// <param name="position">Position</param>
		/// <param name="rotation">Rotation</param>
		/// <param name="scale">Scale (x, y, z)</param>
		/// <param name="enableShading">Enables textures and lighting</param>
		private void DrawDeferredShadedModel(
			GModel gmodel,
			ref Vector3 position,
			ref Quaternion rotation,
			ref Vector3 scale,
			bool enableShading,
            Matrix[] bones)
		{
			Model model = gmodel.model;

			// Calculate transforms
			Matrix world = Matrix.CreateScale(scale)
							* Matrix.CreateFromQuaternion(rotation)
							* Matrix.CreateTranslation(position);
			Matrix[] meshTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(meshTransforms); // get bone transforms

			// Set effect
			Effect effect;
            if (model.Tag is SkinningData && bones != null)
            {
                deferredSkinnedEffect.CurrentTechnique = deferredSkinnedEffect.Techniques["DeferredSkinned"];
                effect = deferredSkinnedEffect;
                effect.Parameters["xBones"].SetValue(bones);
            }
			else if (gmodel.Texture != null && renderState == RenderState.Deferred)
			{
				effect = deferredTextureEffect;
				effect.Parameters["xTexture"].SetValue(gmodel.Texture);
			}
			else if (renderState == RenderState.Deferred)
			{
				deferredEffect.CurrentTechnique = deferredEffect.Techniques["Deferred"];
				effect = deferredEffect;
			}
            else // shadow
			{
				deferredEffect.CurrentTechnique = deferredEffect.Techniques["DeferredLightMap"];
				effect = deferredEffect;
			}

			// Emissive
			if (gmodel.EmissiveTexture != null)
			{
				effect.Parameters["xEnableEmissiveTexture"].SetValue(1);
				effect.Parameters["xEmissiveTex"].SetValue(gmodel.EmissiveTexture);
				effect.Parameters["xEmissive"].SetValue(gmodel.Emissive);
			}
			else
			{
				effect.Parameters["xEnableEmissiveTexture"].SetValue(0);
				effect.Parameters["xEmissive"].SetValue(gmodel.Emissive);
			}

			// Render the mesh
			int iMeshColor = 0;
			effect.Parameters["xWorld"].SetValue(world);
			foreach (ModelMesh mesh in model.Meshes)
			{
				if (bones != null)
					effect.Parameters["xBone"].SetValue(bones[mesh.ParentBone.Index]);
				else
					effect.Parameters["xBone"].SetValue(meshTransforms[mesh.ParentBone.Index]);
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					if (gmodel.MaterialColors != null)
						effect.Parameters["xColor"].SetValue(gmodel.MaterialColors[iMeshColor++]);
					effect.CurrentTechnique.Passes[0].Apply(); // Deferred and DeferredTextured are both single-pass

					Device.Indices = part.IndexBuffer;
					Device.SetVertexBuffer(part.VertexBuffer);
					Device.DrawIndexedPrimitives(
						PrimitiveType.TriangleList,
						part.VertexOffset,
						0,
						part.NumVertices,
						part.StartIndex,
						part.PrimitiveCount);

					// draw call count
					nDrawCalls++;
				}
			}
		}
	#endregion
	}
}

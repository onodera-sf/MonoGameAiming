using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;

namespace Aiming
{
	public class AimingGame : Game
	{
		#region 定数

		/// <summary>猫が動くスピード。これはフレームあたりのピクセル数です。</summary>
		const float CatSpeed = 10.0f;

		/// <summary>スポットライトが回転する速度。これはフレームあたりのラジアンで表されます。</summary>
		const float SpotlightTurnSpeed = 0.025f;

		#endregion --定数

		#region フィールド

		readonly GraphicsDeviceManager _graphics;

		/// <summary>画像を表示するための SpriteBatch です。</summary>
		SpriteBatch _spriteBatch;

		/// <summary>スポットライトのテクスチャー(画像)です。</summary>
		Texture2D _spotlightTexture;

		/// <summary>スポットライトの位置です。</summary>
		Vector2 _spotlightPosition = new Vector2();

		/// <summary>スポットライトの中心位置です、ここを中心に回転します。</summary>
		Vector2 _spotlightOrigin = new Vector2();

		/// <summary>スポットライトが現在向いている角度。単位はラジアンです。値 0 は右を指します。</summary>
		float _spotlightAngle = 0.0f;


		/// <summary>猫のテクスチャー(画像)です。</summary>
		Texture2D _catTexture;

		/// <summary>猫の位置です。</summary>
		Vector2 _catPosition = new Vector2();

		/// <summary>猫の中心位置です。</summary>
		Vector2 _catOrigin = new Vector2();

		#endregion --フィールド


		public AimingGame()
		{
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			_graphics.PreferredBackBufferWidth = 320;
			_graphics.PreferredBackBufferHeight = 480;

			// フルスクリーンにしたい場合はコメントを外してください。
			//graphics.IsFullScreen = true;
		}

		protected override void Initialize()
		{
			base.Initialize();

			// base.Initialize が完了すると、GraphicsDevice が作成され、ビューポートの大きさがわかります。
			// スポットライトを画面の中央に配置する必要があるため、ビューポートを使用してそれがどこにあるかを計算します。
			Viewport vp = _graphics.GraphicsDevice.Viewport;
			_spotlightPosition.X = vp.X + vp.Width / 2;
			_spotlightPosition.Y = vp.Y + vp.Height / 2;

			// もう一度ビューポートサイズを使用して、今度は猫を画面に配置します。位置は x=1/4 y=1/2 です。
			_catPosition.X = vp.X + vp.Width / 4;
			_catPosition.Y = vp.Y + vp.Height / 2;
		}

		protected override void LoadContent()
		{
			// テクスチャをロードし、スプライトバッチを作成します。
			_spotlightTexture = Content.Load<Texture2D>("spotlight");
			_catTexture = Content.Load<Texture2D>("cat");
			_spriteBatch = new SpriteBatch(_graphics.GraphicsDevice);

			// テクスチャをロードしたので、それらを使用して、描画時に使用するいくつかの値を計算できます。
			// スポットライトを描くときは、光源の周りを回転する必要があります。
			// 今回用意した画像は左中央が光源なのでその位置を中心位置として設定します。
			_spotlightOrigin.X = 0;
			_spotlightOrigin.Y = _spotlightTexture.Height / 2;

			// 猫の中心位置を決定します。とりあえず画像の真ん中とします。
			_catOrigin.X = _catTexture.Width / 2;
			_catOrigin.Y = _catTexture.Height / 2;
		}

		protected override void Update(GameTime gameTime)
		{
			HandleInput();

			// 猫が画面外に出ないように制御します。
			Viewport vp = _graphics.GraphicsDevice.Viewport;
			_catPosition.X = MathHelper.Clamp(_catPosition.X, vp.X, vp.X + vp.Width);
			_catPosition.Y = MathHelper.Clamp(_catPosition.Y, vp.Y, vp.Y + vp.Height);

			// TurnToFace 関数を使用して、_spotlightAngle を更新して猫の方を向くようにします。
			_spotlightAngle = TurnToFace(_spotlightPosition, _catPosition, _spotlightAngle, SpotlightTurnSpeed);

			base.Update(gameTime);
		}

		/// <summary>
		/// オブジェクトの位置、ターゲットの位置、現在の角度、および最大回転速度を指定して、オブジェクトが直面する必要のある角度を計算します。
		/// </summary>
		/// <param name="position">オブジェクトの位置。ここではスポットライトの位置。</param>
		/// <param name="faceThis">ターゲットの位置。ここでは猫の位置。</param>
		/// <param name="currentAngle">現在の角度。</param>
		/// <param name="turnSpeed">最大回転速度。</param>
		/// <returns>決定された角度。</returns>
		private static float TurnToFace(Vector2 position, Vector2 faceThis, float currentAngle, float turnSpeed)
		{
			// この図を参照してください。
			// 
			//      C 
			//     /|
			//    / |
			//   /  | y
			//  / o |
			// S----
			//     x
			// 
			// ここで、S はスポットライトの位置、C は猫の位置、o は猫を指すためにスポットライトが向いている角度です。 o の値を知る必要があります。
			// これには三角法を使用して算出します。
			// 
			//      tan(theta)       = 高さ / 底辺
			//      tan(o)           = y / x
			// 
			// この方程式の両辺のアークタンジェントを取ると
			// 
			//      arctan( tan(o) ) = arctan( y / x )
			//      o                = arctan( y / x )
			// 
			// したがって、x と y を使用して、「desiredAngle」である o を見つけることができます。 x と y は、2つのオブジェクト間の位置の違いにすぎません。
			float x = faceThis.X - position.X;
			float y = faceThis.Y - position.Y;

			// Atan2 関数を使用します。 Atanは、y / x のアークタンジェントを計算し、x と y の符号を使用して、結果を入れるデカルト象限を決定するという追加の利点があります。
			// https://docs.microsoft.com/dotnet/api/system.math.atan2
			float desiredAngle = (float)Math.Atan2(y, x);

			// これで猫を向くために必要な設定角度がわかりました。turnSpeed (回転スピード) に制約されていなければ簡単です。desiredAngle を返すだけです。
			// 代わりに回転量を計算し、それが turnSpeed を超えないようにする必要があります。

			// まず、WrapAngle を使用して、-Pi から Pi（-180度から180度）の結果を取得し、どれだけ回転させたいかを判断します。
			// これは猫の方向に向くのに必要な回転角度です。
			float difference = WrapAngle(desiredAngle - currentAngle);

			// -turnSpeed と turnSpeed の間にクランプします。
			// 要は１フレームの回転角度上限を超えないようにします。
			difference = MathHelper.Clamp(difference, -turnSpeed, turnSpeed);

			// したがって、ターゲットに最も近いのは currentAngle + difference です。 もう一度 WrapAngle を使用して、それを返します。
			return WrapAngle(currentAngle + difference);
		}

		/// <summary>
		/// -Pi と Pi の間のラジアンで表される角度を返します。
		/// 例えば degree で -200°なら +360°して 160°とします。反対側も同様です。
		/// </summary>
		private static float WrapAngle(float radians)
		{
			while (radians < -MathHelper.Pi)
			{
				radians += MathHelper.TwoPi;
			}
			while (radians > MathHelper.Pi)
			{
				radians -= MathHelper.TwoPi;
			}
			return radians;
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice device = _graphics.GraphicsDevice;

			device.Clear(Color.Black);

			// 猫を描画します。
			_spriteBatch.Begin();
			_spriteBatch.Draw(_catTexture, _catPosition, null, Color.White, 0.0f, _catOrigin, 1.0f, SpriteEffects.None, 0.0f);
			_spriteBatch.End();

			// 加算合成でスプライトバッチを開始し、スポットライトを当てます。 加算合成は、ライトや火などの効果に非常に適しています。
			_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
			_spriteBatch.Draw(_spotlightTexture, _spotlightPosition, null, Color.White, _spotlightAngle, _spotlightOrigin, 1.0f, SpriteEffects.None, 0.0f);
			_spriteBatch.End();

			base.Draw(gameTime);
		}


		/// <summary>
		/// 入力を処理します。
		/// </summary>
		void HandleInput()
		{
			KeyboardState currentKeyboardState = Keyboard.GetState();
			GamePadState currentGamePadState = GamePad.GetState(PlayerIndex.One);
			MouseState currentMouseState = Mouse.GetState();
			TouchCollection currentTouchState = TouchPanel.GetState();

			// ゲーム終了操作を確認します。
			if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
					currentGamePadState.Buttons.Back == ButtonState.Pressed)
			{
				Exit();
			}

			// ユーザーが猫を動かしたいかどうかを確認します。 catMovement というベクトルを作成します。これは、すべてのユーザーの入力の合計を格納します。
			Vector2 catMovement = currentGamePadState.ThumbSticks.Left;

			// y を反転：スティックでは、下は -1 ですが、画面では、下がプラスです。
			catMovement.Y *= -1;

			if (currentKeyboardState.IsKeyDown(Keys.Left) ||
					currentGamePadState.DPad.Left == ButtonState.Pressed)
			{
				catMovement.X -= 1.0f;
			}
			if (currentKeyboardState.IsKeyDown(Keys.Right) ||
					currentGamePadState.DPad.Right == ButtonState.Pressed)
			{
				catMovement.X += 1.0f;
			}
			if (currentKeyboardState.IsKeyDown(Keys.Up) ||
					currentGamePadState.DPad.Up == ButtonState.Pressed)
			{
				catMovement.Y -= 1.0f;
			}
			if (currentKeyboardState.IsKeyDown(Keys.Down) ||
					currentGamePadState.DPad.Down == ButtonState.Pressed)
			{
				catMovement.Y += 1.0f;
			}

			// タッチポイントに向かって移動します。 CatSpeed からタッチポイントまでの距離内に入ると、猫の速度を落とします。
			float smoothStop = 1;

			//if (currentTouchState != null )
			{
				if (currentTouchState.Count > 0)
				{
					Vector2 touchPosition = currentTouchState[0].Position;
					if (touchPosition != _catPosition)
					{
						catMovement = touchPosition - _catPosition;
						float delta = CatSpeed - MathHelper.Clamp(catMovement.Length(), 0, CatSpeed);
						smoothStop = 1 - delta / CatSpeed;
					}
				}
			}

			Vector2 mousePosition = new Vector2(currentMouseState.X, currentMouseState.Y);
			if (currentMouseState.LeftButton == ButtonState.Pressed && mousePosition != _catPosition)
			{
				catMovement = mousePosition - _catPosition;
				float delta = CatSpeed - MathHelper.Clamp(catMovement.Length(), 0, CatSpeed);
				smoothStop = 1 - delta / CatSpeed;
			}

			// ユーザーの入力を正規化して、猫が CatSpeed より速く進むことができないようにします。
			if (catMovement != Vector2.Zero)
			{
				catMovement.Normalize();
			}

			_catPosition += catMovement * CatSpeed * smoothStop;
		}

	}
}

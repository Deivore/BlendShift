Shader "ColorCycling/BlendShift" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)

		_MainTex("Indices", 2D) = "white" {}
		_Palette("Palette", 2D) = "white" {}

		_NumCycles("Number of Cycles", Int) = 0
		_DoBlendShift("Blendshift", Int) = 0
	}

	SubShader
	{

		Tags
		{
			"Queue" = "Transparent"
		}

		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
//#pragma exclude_renderers d3d11 gles
			#pragma enable_d3d11_debug_symbols
			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _Color;
			sampler2D _MainTex;
			sampler2D _Palette;
			fixed4 _FinalPalette[256];
			int	_NumCycles;
			fixed4 _Cycles[20];
			int _DoBlendShift;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = float4(v.uv.xy, 0, 0);
				o.color = float4(0, 0, v.uv.y, 1);
				return o;
			}

			//externally defined color

			// pixel shader
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 fixedIndex = tex2D(_MainTex, i.uv);
				uint index = fixedIndex.r*255;
				return _FinalPalette[index];

				/*
				fixed2 indexVal;
				fixed4 palette_color;
				for (int i = 0; i < _NumCycles; i++)
				{
					//cycles:
					//0 = reverse, 1= rate, 2 = low, 3 = high?
					if (_Cycles[i].g != 6825 && index >= _Cycles[i].b && index <= _Cycles[i].a)
					{
						uint offset = index - _Cycles[i].b;
						uint length = _Cycles[i].a - _Cycles[i].b + 1;
						uint intTime = _Time.y*5;
						uint newOffset = abs((offset - intTime) % length);
						index = newOffset + _Cycles[i].b;
						uint nextIndex = (index + 1) % length;
						//index = 48;
						indexVal = (0.5, index / 255.0);
						palette_color = tex2D(_Palette, indexVal);
						if (false)
						{
							uint nextIndex = (index + 1) % length;
							fixed2 nextIndexVal = (0.5, index / 255.0);
							fixed4 next_palette_color = tex2D(_Palette, indexVal);
							float timeFraction = abs(intTime - _Time.y*-5);
							next_palette_color = next_palette_color - palette_color;
							palette_color = palette_color + (timeFraction*next_palette_color);
						}
						return palette_color;
					}
				}
				//fixed4 palette_color = fixed4(0.5,0.5,0.5,1);
				indexVal = (0.5, index / 255.0);
				palette_color = tex2D(_Palette, indexVal);
				return palette_color; // just return it
				*/
			}
			ENDCG
		}
	}
}
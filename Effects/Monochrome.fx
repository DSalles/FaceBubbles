//--------------------------------------------------------------------------------------
// 
// WPF ShaderEffect HLSL -- MonoChromeEffect
//
//--------------------------------------------------------------------------------------

//-----------------------------------------------------------------------------------------
// Shader constant register mappings (scalars - float, double, Point, Color, Point3D, etc.)
//-----------------------------------------------------------------------------------------

float4 filterColor : register(C0);

//--------------------------------------------------------------------------------------
// Sampler Inputs (Brushes, including ImplicitInput)
//--------------------------------------------------------------------------------------

sampler2D  implicitInputSampler : register(S0);

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------

float4 main(float2 uv : TEXCOORD) : COLOR
{
   float2 texuv = uv;
   float4 srcColor = tex2D(implicitInputSampler, texuv);
   float4 luminance = srcColor;

   if(srcColor.a != 0)
	   {
	   luminance -= filterColor;
	   if(luminance.r < 0)
	   {luminance.r = abs(luminance.r) * srcColor.a;}
	   if(luminance.g < 0)
	   {luminance.g = abs(luminance.g)* srcColor.a; }
	   if(luminance.b < 0)
	   {luminance.b = abs(luminance.b) * srcColor.a;}
	   }

	   luminance.r *=.5;

   luminance.a = 0;


   return luminance*.6;
}



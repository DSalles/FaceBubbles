//--------------------------------------------------------------------------------------
// 
// WPF ShaderEffect HLSL -- CircularVignette
//
//--------------------------------------------------------------------------------------

//-----------------------------------------------------------------------------------------
// Shader constant register mappings (scalars - float, double, Point, Color, Point3D, etc.)
//-----------------------------------------------------------------------------------------

//float2 radii : register(C0);
//float2 center : register(C1);
//float  amount : register(C2);
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
   float2 origUv = uv;
   float2 ray = origUv - (0.5, 0.5);
   float2 rt = ray /(0.5,0.5);      
   float lengthRt = length(rt);
   float4 srcColor = tex2D(implicitInputSampler, origUv);  
   float4 returnColor = srcColor;
   float4 midColor = srcColor;
   float4 hiColor = srcColor;
   float4 loColor = srcColor;
   float4 invertedFilterColor = 1 - filterColor;
   float loThreshold = 0.09;
   float midThreshold = 0.3;
   float hiThreshold = 0.8;
   
				
      // Outside of radii, erase the pixels.  Radii is ellipse radii, so width x height radius 
   if (lengthRt > 1)
   {
	   returnColor.r = 0;
	   returnColor.g = 0;
	   returnColor.b = 0;
   }
   else
   {
   if (srcColor.g < loThreshold)
   {
   returnColor = filterColor*0.2;
   }

   else
   {
   
      // if green is dark, set colors to inverted filterColor without green
	  if (srcColor.g < midThreshold)

		{ 			
			loColor.r = invertedFilterColor.r *.2;
			loColor.g = 0;
			loColor.b = invertedFilterColor.b*.3;	
			
			returnColor = loColor*((srcColor.g - loThreshold)*1/(midThreshold-loThreshold)) + filterColor*((srcColor.g - midThreshold)*(1/midThreshold)*-1);		

		}
	  
		 else
		{
			if(srcColor.g < hiThreshold)
				{					
			midColor.r=srcColor.r*filterColor.r;
			midColor.g = srcColor.g*filterColor.g;
			midColor.b = srcColor.b*filterColor.b;
			hiColor=1;	
			returnColor =  hiColor*((srcColor.g - midThreshold)*1/(hiThreshold-midThreshold)) + midColor*((srcColor.g - hiThreshold)*(1/hiThreshold-midThreshold)*-1);				
				}
				else
				{
				returnColor = 1;
				}
		}
	}
	   returnColor.a = 0;
	  
   }
   
   return returnColor*.5;
}




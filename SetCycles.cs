using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetCycles : MonoBehaviour
{

    //0 = reverse, 1= rate, 2 = low, 3 = high
    public Vector4[] cycles;

    public Vector4[] truePalette;
    public Vector4[] framePalette;
    public bool BlendShift = true;
    private Material cyclingMaterial;

    private void Awake()
    {
        truePalette = new Vector4[256];
        framePalette = new Vector4[256];

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        cyclingMaterial = sr.material;
        cyclingMaterial.SetVectorArray("_Cycles", cycles);

        Texture2D palette = (Texture2D)cyclingMaterial.GetTexture("_Palette");
        for (int i = 0; i < 256; i++)
        {
            truePalette[i] = palette.GetPixel(i, 0);
            framePalette[i] = truePalette[i];
        }

        System.Console.WriteLine("BlendShift cycle count: " + cycles.GetLength(0));
    }


    void Update()
    {
        //0 = reverse, 1= rate, 2 = low, 3 = high?
        double ticks = System.DateTime.Now.Ticks;
        for ( int h = 0; h < cycles.Length; h++)
        {
            Vector4 cycle = cycles[h];
            int cycleSize = ((int)cycle[3] + 1) - (int)cycle[2];
            if (cycle[1] != 6825 /*&& cycle[0] < magicReverseNumber*/ )
            {
                //Start Huckaby secret sauce
                float cycleRate = cycle[1] / 280;
                float cycleOffset = 0f;
                if (cycle[0] < 3 || cycle[0] == 256)
                {
                    // standard cycle
                    cycleOffset = (float)((ticks / (10000000 / cycleRate)) % cycleSize);//DFLOAT_MOD((ticks / (1000 / cycleRate)), cycleSize);
                    
                    /*if(h == 1)
                    {
                        Debug.Log("Index65");
                        Debug.Log(" ticks: " + ticks);
                        Debug.Log(" cycleRate: " + cycleRate);
                        Debug.Log(" cycleSize: " + cycleSize);
                        Debug.Log(" cycleOffset: " + cycleOffset);

                        System.Console.WriteLine("Index65");
                        System.Console.WriteLine(" ticks: " + ticks);
                        System.Console.WriteLine(" cycleRate: " + cycleRate);
                        System.Console.WriteLine(" cycleSize: " + cycleSize);
                        System.Console.WriteLine(" cycleOffset: " + cycleOffset);
                    }*/
                }
                else if (cycle[0] == 3)
                {
                    // ping-pong
                    cycleOffset = (float)((ticks / (10000000 / cycleRate)) % (cycleSize*2));
                    if (cycleOffset >= cycleSize) cycleOffset = (cycleSize * 2) - cycleOffset;
                }
                else if (cycle[0] < 6)
                {
                    // sine wave
                    cycleOffset = (float)((ticks / (10000000 / cycleRate)) % cycleSize);
                    cycleOffset = Mathf.Sin((cycleOffset * 3.1415926f * 2) / cycleSize) + 1;
                    if (cycle[0] == 4) cycleOffset *= (cycleSize / 4);
                    else if (cycle[0] == 5) cycleOffset *= (cycleSize / 2);
                }

                //End Huckaby secret sauce

                int index_offset = (int)Mathf.Floor(cycleOffset);
                int baseIndex = (int)cycle[2];
                if (cycle[0] != 2)
                {
                    for (int i = 0; i < cycleSize; i++)
                    {
                        if (BlendShift)
                        {
                            int swapIndex = ((index_offset + i) % cycleSize) + baseIndex;
                            int originIndex = baseIndex + i;
                            int originIndexNext = baseIndex + (i+1)% cycleSize;
                            framePalette[swapIndex] = truePalette[originIndexNext] - (truePalette[originIndexNext] - truePalette[originIndex]) * (cycleOffset%1);
                            /*if(swapIndex == 65)
                            {
                                Debug.Log("Index65");
                                Debug.Log(" ticks: " + ticks);
                                Debug.Log(" cycleRate: " + cycleRate);
                                Debug.Log(" cycleOffset: " + cycleOffset);
                                Debug.Log(" cycleSize: " + cycleSize);
                                Debug.Log("  r: " + framePalette[swapIndex].x);
                                Debug.Log("  b: " + framePalette[swapIndex].y);
                                Debug.Log("  g: " + framePalette[swapIndex].z);
                                Debug.Log("  a: " + framePalette[swapIndex].w);

                                System.Console.WriteLine("Index65");
                                System.Console.WriteLine(" ticks: " + ticks);
                                System.Console.WriteLine(" cycleRate: " + cycleRate);
                                System.Console.WriteLine(" cycleOffset: " + cycleOffset);
                                System.Console.WriteLine(" cycleSize: " + cycleSize);
                                System.Console.WriteLine("  r: " + framePalette[swapIndex].x);
                                System.Console.WriteLine("  b: " + framePalette[swapIndex].y);
                                System.Console.WriteLine("  g: " + framePalette[swapIndex].z);
                                System.Console.WriteLine("  a: " + framePalette[swapIndex].w);
                            }*/
                        }
                        else
                        {
                            int swapIndex = ((index_offset + i) % cycleSize) + baseIndex;
                            int originIndex = baseIndex + i;
                            framePalette[swapIndex] = truePalette[originIndex];
                        }
                    }
                }
                else //reverse
                {
                    for (int i = (int)cycle[2]; i < (int)cycle[3] + 1; i++)
                    {
                        if (BlendShift)
                        {
                            int swapIndex = ((cycleSize - i) % cycleSize) + baseIndex;
                            int originIndex = baseIndex + i;
                            int originIndexNext = baseIndex + (2*cycleSize-i - 1) % cycleSize;
                            framePalette[swapIndex] = truePalette[originIndexNext] - (truePalette[originIndexNext] - truePalette[originIndex]) * (cycleOffset % 1);
                        }
                        else
                        {
                            int swapIndex = ((cycleSize - i) % cycleSize) + baseIndex;
                            int originIndex = baseIndex + i;
                            framePalette[swapIndex] = truePalette[originIndex];
                        }

                    }
                }
            }
        }

        SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
        UnityEngine.Material m = sr.material;
        cyclingMaterial.SetVectorArray("_FinalPalette", framePalette);
    }

    private short PRECISION = 100;

    // this utility function allows for variable precision floating point modulus
    float DFLOAT_MOD(float a, float b) { return (Mathf.Floor(a * PRECISION) % Mathf.Floor(b * PRECISION)) / PRECISION; }

}

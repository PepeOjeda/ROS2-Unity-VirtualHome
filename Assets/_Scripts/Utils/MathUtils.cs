using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtils
{
    static System.Random rng = new();
    public static double RandGaussian(double mean, double stdDev)
    {
        //box-muller transform
        double u1 = 1.0 - rng.NextDouble(); //uniform(0,1] random doubles
        double u2 = 1.0 - rng.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
        return mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)

    }
}

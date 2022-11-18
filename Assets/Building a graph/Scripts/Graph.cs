using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    [SerializeField] 
    private Transform pointPrefab;

    [SerializeField, Range(10, 100)]
    private int resolution = 10;

    [SerializeField]
    private FunctionLibrary.FunctionName functionName;

    [SerializeField, Min(0f)] 
    private float functionDuration = 1f, transitionDuration = 1f;

    private bool isTransitioning;
    private FunctionLibrary.FunctionName transitionFunction;
    
    private Transform[] points;

    private float duration;
    
    private void Awake()
    {
        float step = 2f / this.resolution;
        Vector3 scale = step * Vector3.one;
        this.points = new Transform[this.resolution * this.resolution];
        for (var i = 0; i < this.points.Length; i++)
        {
            
            Transform point = this.points[i] = Instantiate(this.pointPrefab);
            point.localScale = scale;
            
            point.SetParent(this.transform, false);
        }
    }

    private void Update()
    {
        this.duration += Time.deltaTime;
        
        if(this.duration >= this.transitionDuration && this.isTransitioning)
        {
            this.duration -= this.transitionDuration;
            this.isTransitioning = false;
        }
        
        if (this.isTransitioning)
        {
            this.UpdateFunctionTransition();
        } 
        else if(this.duration >= this.functionDuration)
        {
            this.duration -= this.functionDuration;
            this.transitionFunction = this.functionName;
            this.functionName = (FunctionLibrary.FunctionName)UnityEngine.Random.Range(0, 6);
            this.isTransitioning = true;
        }
        else
        {
            this.UpdateFunction();
        }
    }

    private void UpdateFunctionTransition()
    {
        FunctionLibrary.Function f0 = FunctionLibrary.GetFunction(this.transitionFunction);
        FunctionLibrary.Function f1 = FunctionLibrary.GetFunction(this.functionName);
        
        float progress = this.duration / this.transitionDuration;
        float time = Time.time;
        float step = 2f / this.resolution;
        float v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < this.points.Length; i++, x++)
        {
            if (x == this.resolution)
            {
                x = 0;
                z++;
                v = (z + 0.5f) * step - 1f;
            }

            float u = (x + 0.5f) * step - 1f;
            this.points[i].localPosition = FunctionLibrary.Morb(u, v, time, f0, f1, progress);
        }
    }

    private void UpdateFunction()
    {
        FunctionLibrary.Function f = FunctionLibrary.GetFunction(this.functionName);
        float time = Time.time;
        float step = 2f / this.resolution;
        float v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < this.points.Length; i++, x++)
        {
            if (x == this.resolution)
            {
                x = 0;
                z++;
                v = (z + 0.5f) * step - 1f;
            }

            float u = (x + 0.5f) * step - 1f;
            this.points[i].localPosition = f(u, v, time);
        }
    }
}

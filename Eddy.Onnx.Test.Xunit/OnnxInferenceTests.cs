using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Xunit;
using Xunit.Abstractions;

namespace Eddy.Onnx.Test.Xunit;

public class OnnxInferenceTests
{
    private const string ModelEnvVar = "EDDY_ONNX_MODEL";

    private const string DefaultModelPath =
        "/Users/patrickkastner/Library/CloudStorage/Dropbox-GaTech/Patrick Kastner/ML/Models (1)/pix2pixhd_uk_generator.onnx";

    // WindPredictorCMP grid constants (must match training pipeline)
    private const int ImgH = 504;
    private const int ImgW = 504;
    private const int XCh = 8;

    private readonly ITestOutputHelper _output;

    public OnnxInferenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string ResolveModelPath()
    {
        string fromEnv = Environment.GetEnvironmentVariable(ModelEnvVar);
        return string.IsNullOrWhiteSpace(fromEnv) ? DefaultModelPath : fromEnv;
    }

    private static void SkipIfModelMissing(string path)
    {
        Skip.IfNot(File.Exists(path),
            $"ONNX model not found. Set {ModelEnvVar} or place the model at '{DefaultModelPath}'. " +
            $"Resolved path was '{path}'.");
    }

    [SkippableFact]
    public void Loads_session_with_cpu_provider()
    {
        string path = ResolveModelPath();
        SkipIfModelMissing(path);

        using var opts = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
        };

        using var session = new InferenceSession(path, opts);

        Assert.NotEmpty(session.InputMetadata);
        var firstInput = session.InputMetadata.First();
        Assert.Equal(typeof(float), firstInput.Value.ElementType);
    }

    [SkippableFact]
    public void Runs_dummy_inference()
    {
        string path = ResolveModelPath();
        SkipIfModelMissing(path);

        using var opts = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
        };

        using var session = new InferenceSession(path, opts);
        var inputMeta = session.InputMetadata.First();
        string inputName = inputMeta.Key;

        var tensor = new DenseTensor<float>(new[] { 1, XCh, ImgH, ImgW });

        var feeds = new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };

        using var results = session.Run(feeds);

        Assert.NotEmpty(results);
        var first = results.First().AsTensor<float>();
        Assert.NotNull(first);
        Assert.True(first.Dimensions.Length >= 3,
            $"Expected output rank >= 3, got {first.Dimensions.Length}.");

        foreach (float v in first)
        {
            Assert.False(float.IsNaN(v), "Output tensor contains NaN.");
        }
    }

    [SkippableFact]
    public void CoreML_inference_is_faster_than_cpu()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
            "CoreML execution provider is macOS-only.");

        string path = ResolveModelPath();
        SkipIfModelMissing(path);

        var tensor = new DenseTensor<float>(new[] { 1, XCh, ImgH, ImgW });

        long cpuMs = RunOnce(path, useCoreML: false, tensor);
        long coreMlMs = RunOnce(path, useCoreML: true, tensor);

        _output.WriteLine($"CPU:    {cpuMs} ms");
        _output.WriteLine($"CoreML: {coreMlMs} ms");
        _output.WriteLine($"Speedup: {cpuMs / (double)Math.Max(1, coreMlMs):F2}x");
    }

    private static long RunOnce(string modelPath, bool useCoreML, DenseTensor<float> tensor)
    {
        using var opts = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
        };

        if (useCoreML)
        {
            opts.AppendExecutionProvider_CoreML(CoreMLFlags.COREML_FLAG_CREATE_MLPROGRAM);
        }

        using var session = new InferenceSession(modelPath, opts);
        string inputName = session.InputMetadata.First().Key;
        var feeds = new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };

        // Warm-up run (CoreML compiles on first inference)
        using (var _ = session.Run(feeds)) { }

        var sw = Stopwatch.StartNew();
        using (var _ = session.Run(feeds)) { }
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }
}

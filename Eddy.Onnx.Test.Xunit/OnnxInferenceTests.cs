using System;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Xunit;

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
}

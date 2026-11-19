using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace ApoGirl.EditorTools
{
    /// <summary>
    /// 테스트 실행이 끝날 때마다 결과 요약을 Temp/ApoGirlTestResults.txt 에 쓴다.
    /// PlayMode 테스트는 도메인 리로드를 거치므로 [InitializeOnLoad]로 매번 다시 등록한다 (자동화·MCP 확인용).
    /// </summary>
    [InitializeOnLoad]
    public static class TestResultWriter
    {
        public const string ResultPath = "Temp/ApoGirlTestResults.txt";

        static TestResultWriter()
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Callbacks());
        }

        private sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"status={result.TestStatus} pass={result.PassCount} fail={result.FailCount} skip={result.SkipCount}");
                Append(result, builder);
                File.WriteAllText(ResultPath, builder.ToString());
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }

            private static void Append(ITestResultAdaptor result, StringBuilder builder)
            {
                if (!result.HasChildren)
                {
                    builder.AppendLine($"{result.TestStatus} {result.FullName} {result.Message}".TrimEnd());
                    return;
                }

                foreach (var child in result.Children)
                {
                    Append(child, builder);
                }
            }
        }
    }
}

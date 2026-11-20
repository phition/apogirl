using System.Linq;
using System.Reflection;
using ApoGirl.Presentation.Dialogue;
using NUnit.Framework;
using Yarn.Unity;

namespace ApoGirl.Tests.PlayMode
{
    /// <summary>
    /// [YarnFunction]으로 선언한 대본 함수가 실행 목록(YarnFunctions.Table)에도 등록돼 있는지 확인한다.
    /// 선언만 있고 등록이 빠지면 대본은 컴파일되지만 실행 중에 "알 수 없는 함수" 오류가 난다.
    /// </summary>
    public class YarnFunctionTableTests
    {
        [Test]
        public void EveryDeclaredYarnFunction_IsRegistered()
        {
            var declared = typeof(YarnFunctions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Select(method => method.GetCustomAttribute<YarnFunctionAttribute>())
                .Where(attribute => attribute != null)
                .Select(attribute => attribute.Name)
                .ToList();

            Assert.IsNotEmpty(declared, "선언된 대본 함수를 찾지 못했다");
            var missing = declared.Where(name => !YarnFunctions.Has(name)).ToList();
            Assert.IsEmpty(missing, "실행 목록에 빠진 대본 함수: " + string.Join(", ", missing));
        }
    }
}

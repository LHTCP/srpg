#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class SrpM1TestRunnerMenu
{
    [MenuItem("SRPG/Run M1 Automated QA (Edit+Play)")]
    static void RunM1AutomatedQa()
    {
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var cb = new SrpM1RunCallbacks(api);
        api.RegisterCallbacks(cb);
        cb.RunEditMode();
        Debug.Log("[SRPG][TestRunner] M1 자동화 QA 시작 (EditMode -> PlayMode)");
    }

    class SrpM1RunCallbacks : ICallbacks
    {
        readonly TestRunnerApi _api;
        bool _editModeRunning;

        public SrpM1RunCallbacks(TestRunnerApi api)
        {
            _api = api;
        }

        public void RunEditMode()
        {
            _editModeRunning = true;
            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                categoryNames = new[] { "SrpM1All" },
            };
            _api.Execute(new ExecutionSettings(filter));
        }

        void RunPlayMode()
        {
            _editModeRunning = false;
            var filter = new Filter
            {
                testMode = TestMode.PlayMode,
                categoryNames = new[] { "SrpM1All" },
            };
            _api.Execute(new ExecutionSettings(filter));
        }

        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            if (_editModeRunning)
            {
                if (HasFailures(result))
                {
                    Debug.LogError("[SRPG][TestRunner] EditMode 실패 감지: PlayMode 실행을 건너뜁니다.");
                    _api.UnregisterCallbacks(this);
                    return;
                }

                Debug.Log("[SRPG][TestRunner] EditMode 완료, PlayMode 실행 시작");
                RunPlayMode();
                return;
            }

            Debug.Log("[SRPG][TestRunner] M1 자동화 QA 완료");
            _api.UnregisterCallbacks(this);
        }

        static bool HasFailures(ITestResultAdaptor result)
        {
            if (result == null)
                return true;
            if (result.FailCount > 0)
                return true;
            if (result.TestStatus == TestStatus.Failed || result.TestStatus == TestStatus.Inconclusive)
                return true;
            if (!result.HasChildren)
                return false;
            foreach (var child in result.Children)
            {
                if (HasFailures(child))
                    return true;
            }
            return false;
        }
    }
}
#endif

using System.Threading.Tasks;
using System.Collections.Generic;
using RecognX;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FrontendUI : MonoBehaviour
{
    public TMP_Dropdown taskDropdown;
    public Button captureButton;
    public Button startTaskButton;
    public Button backButton;
    public TextMeshProUGUI feedbackText;
    public Button locateButton;
    public GameObject loadingSpinner;
    public TextMeshProUGUI stepList;
    public TextMeshProUGUI objectList;
    private readonly Dictionary<int, GameObject> objectChecklistItems = new();
    private readonly Dictionary<int, GameObject> stepChecklistItems = new();

    private List<RecognX.TaskResponse> loadedTasks;
    private RecognX.ARInstructionManager manager;

    private void Start()
    {
        loadingSpinner.SetActive(true);

        manager = RecognX.ARInstructionManager.Instance;

        manager.OnTasksLoaded += tasks =>
        {
            loadedTasks = tasks;
            taskDropdown.ClearOptions();
            taskDropdown.AddOptions(tasks.ConvertAll(t => new TMP_Dropdown.OptionData(t.title)));
            loadingSpinner.SetActive(false);
        };

        manager.OnInstructionFeedback += response =>
        {
            Debug.Log($"🧠 Feedback received: {response.response}");
            if (string.IsNullOrEmpty(response.response) && response.task_completed)
            {
                feedbackText.text = "✅ Task completed!";
            }
            else
            {
                feedbackText.text = response.response;
            }
        };

        manager.OnTaskStateChanged += UpdateUIForState;

        manager.OnLocalizationProgressUpdated += UpdateObjects;
        manager.OnRelevantObjectsUpdated += UpdateObjects;

        manager.OnStepProgressUpdated += UpdateStepList;

        taskDropdown.onValueChanged.AddListener(OnTaskSelected);
        captureButton.onClick.AddListener(() =>
        {
            loadingSpinner.SetActive(true);
            manager.TrackStepAsync().ContinueWith(task =>
            {
                if (task.IsFaulted && task.Exception != null)
                    Debug.LogException(task.Exception);
                loadingSpinner.SetActive(false);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        });

        startTaskButton.onClick.AddListener(() =>
        {
            clearObjects();
            clearSteps();

            manager.StartTracking();
        });
        backButton.onClick.AddListener(ResetToTaskSelection);

        locateButton.onClick.AddListener(() =>
        {
            loadingSpinner.SetActive(true);
            manager.CaptureAndLocalize().ContinueWith(task =>
            {
                if (task.IsFaulted && task.Exception != null)
                    Debug.LogException(task.Exception);
                loadingSpinner.SetActive(false);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        });

        UpdateUIForState(RecognX.TaskState.Idle);
    }

    private void OnTaskSelected(int index)
    {
        loadingSpinner.SetActive(true);
        var taskId = loadedTasks[index].id;
        manager.SelectTaskAsync(taskId).ContinueWith(task =>
        {
            if (task.IsFaulted && task.Exception != null)
                Debug.LogException(task.Exception);
            loadingSpinner.SetActive(false);
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void UpdateUIForState(RecognX.TaskState state)
    {
        bool isIdle = state == RecognX.TaskState.Idle;
        bool taskSummary = state == RecognX.TaskState.TaskSummary;
        bool tracking = state == RecognX.TaskState.Tracking || state == RecognX.TaskState.Completed;

        taskDropdown.gameObject.SetActive(isIdle);
        backButton.gameObject.SetActive(!isIdle);
        startTaskButton.gameObject.SetActive(taskSummary);
        captureButton.gameObject.SetActive(tracking);
        feedbackText.gameObject.SetActive(tracking);
        locateButton.gameObject.SetActive(tracking);

        if (taskSummary)
        {
            UpdateObjects(manager.GetAllObjectsForCurrentTask());
            UpdateStepList(manager.GetAllStepsForCurrentTask());
        }
    }

    private void UpdateObjects(Dictionary<int, (string label, int required, int found)> summary)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kvp in summary)
        {
            var (label, required, found) = kvp.Value;
            string color = (found >= required) ? "#00FF00" : "#FFFFFF";

            sb.Append($"<color={color}>{label}: {found}/{required}</color>\n\n");
        }

        objectList.text = sb.ToString();
    }

    private void UpdateStepList(List<(int stepId, string description, bool completed)> steps)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < steps.Count; i++)
        {
            var (stepId, description, completed) = steps[i];
            string color = completed ? "#00FF00" : "#FFFFFF";
            sb.Append($"<color={color}>{i + 1}: {description.Trim()}</color>\n\n");
        }

        stepList.text = sb.ToString();
    }

    private void clearObjects()
    {
        if (objectList != null)
        {
            objectList.text = string.Empty;
        }
    }

    private void clearSteps()
    {
        if (stepList != null)
        {
            stepList.text = string.Empty;
        }
    }

    private void ResetToTaskSelection()
    {
        manager.ResetToTaskSelection();
        feedbackText.text = "";

        clearSteps();
        clearObjects();
    }
}
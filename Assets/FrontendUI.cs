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
    public Transform objectChecklistContainer;
    public Transform stepChecklistContainer;
    public GameObject checklistItemPrefab;
    private readonly Dictionary<int, GameObject> objectChecklistItems = new();
    private readonly Dictionary<int, GameObject> stepChecklistItems = new();

    private List<RecognX.TaskResponse> loadedTasks;
    private RecognX.ARInstructionManager manager;

    private void Start()
    {
        manager = RecognX.ARInstructionManager.Instance;

        manager.OnTasksLoaded += tasks =>
        {
            loadedTasks = tasks;
            taskDropdown.ClearOptions();
            taskDropdown.AddOptions(tasks.ConvertAll(t => new TMP_Dropdown.OptionData(t.title)));
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
        captureButton.onClick.AddListener(() => { manager.TrackStepAsync(); });

        startTaskButton.onClick.AddListener(() =>
        {
            clearObjects();
            clearSteps();

            manager.startTracking();
        });
        backButton.onClick.AddListener(ResetToTaskSelection);

        locateButton.onClick.AddListener(() => { manager.CaptureAndLocalize(); });

        UpdateUIForState(RecognX.TaskState.Idle);
    }

    private void OnTaskSelected(int index)
    {
        var taskId = loadedTasks[index].id;
        manager.SelectTaskAsync(taskId);
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
        Debug.Log("UpdateObjects called");
        clearObjects();
        foreach (var kvp in summary)
        {
            int yoloId = kvp.Key;
            var (label, required, found) = kvp.Value;

            if (!objectChecklistItems.TryGetValue(yoloId, out GameObject item))
            {
                item = Instantiate(checklistItemPrefab, objectChecklistContainer);
                objectChecklistItems[yoloId] = item;
            }

            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{label}: {found}/{required}";
                text.color = found >= required ? Color.green : Color.white;
            }
        }
    }

    private void UpdateStepList(List<(int stepId, string description, bool completed)> steps)
    {
        clearSteps();
        foreach (var (stepId, description, completed) in steps)
        {
            GameObject item = Instantiate(checklistItemPrefab, stepChecklistContainer);
            stepChecklistItems[stepId] = item;

            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{stepId + 1}: {description}";
                text.color = completed ? Color.green : Color.white;
            }
        }
    }

    private void clearObjects()
    {
        objectChecklistItems.Clear();
        foreach (Transform child in objectChecklistContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void clearSteps()
    {
        stepChecklistItems.Clear();
        foreach (Transform child in stepChecklistContainer)
        {
            Destroy(child.gameObject);
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
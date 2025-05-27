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

    public Transform checklistContainer;
    public GameObject checklistItemPrefab;
    private readonly Dictionary<int, GameObject> checklistItems = new();

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
            feedbackText.text = response.response;
        };

        manager.OnTaskStateChanged += UpdateUIForState;

        manager.OnLocalizationProgressUpdated += UpdateChecklist;

        taskDropdown.onValueChanged.AddListener(OnTaskSelected);
        captureButton.onClick.AddListener( () =>
        {
            if (manager.CurrentState == RecognX.TaskState.LocatingObjects)
                manager.CaptureAndLocalize();
            else if (manager.CurrentState == RecognX.TaskState.ReadyToTrack || manager.CurrentState == RecognX.TaskState.Tracking)
                manager.TrackStepAsync();
        });

        startTaskButton.onClick.AddListener(() => manager.startTracking());
        backButton.onClick.AddListener(ResetToTaskSelection);

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
        bool locating = state == RecognX.TaskState.LocatingObjects;
        bool readyOrTracking = state == RecognX.TaskState.ReadyToTrack || state == RecognX.TaskState.Tracking;

        taskDropdown.gameObject.SetActive(isIdle);
        backButton.gameObject.SetActive(!isIdle);
        startTaskButton.gameObject.SetActive(locating);
        captureButton.gameObject.SetActive(!isIdle);
        feedbackText.gameObject.SetActive(readyOrTracking);
    }

    private void UpdateChecklist(Dictionary<int, (string label, int required, int found)> summary)
    {
        foreach (var kvp in summary)
        {
            int yoloId = kvp.Key;
            var (label, required, found) = kvp.Value;

            if (!checklistItems.TryGetValue(yoloId, out GameObject item))
            {
                item = Instantiate(checklistItemPrefab, checklistContainer);
                checklistItems[yoloId] = item;
            }

            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{label}: {found}/{required}";
                text.color = found >= required ? Color.green : Color.white;
            }
        }
    }

    private void ResetToTaskSelection()
    {
        manager.ResetToTaskSelection();
        feedbackText.text = "";
        
        foreach (Transform child in checklistContainer)
        {
            Destroy(child.gameObject);
        }

        checklistItems.Clear();
    }
}
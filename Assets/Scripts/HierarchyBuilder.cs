using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class HierarchyBuilder : MonoBehaviour
{
    public GameObject rootObject; // Корневой объект для иерархии
    public GameObject buttonPrefab; // Префаб для кнопок
    public Transform contentParent; // Контейнер для кнопок
    public Color rootButtonColor = Color.red; // Цвет кнопки для корневого объекта
    public Color rootButtonTextColor = Color.white; // Цвет текста для корневого объекта
    public Color selectedButtonColor = Color.green; // Цвет кнопки для выделенного объекта
    public Color normalButtonColor = Color.yellow; // Цвет кнопки для выделенного объекта

    public Camera mainCamera; // Камера для фокусировки
    public float transitionDuration = 1.0f; // Длительность перехода камеры
    public float forwardCamera = 10f; // Сдвиг камеры вперед
    public float upCamera = 0f; // Сдвиг камеры вверх

    private Dictionary<Transform, GameObject> buttonDictionary = new Dictionary<Transform, GameObject>();
    private Dictionary<Transform, TransformData> originalTransforms = new Dictionary<Transform, TransformData>();
    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;
    private Coroutine cameraCoroutine;
    private Transform currentTarget;

    void Start()
    {
        if (rootObject != null && buttonPrefab != null && contentParent != null && mainCamera != null)
        {
            // Сохраняем исходные трансформы
            SaveOriginalTransforms(rootObject.transform);
            // Создаем кнопку для корневого объекта
            CreateButton(rootObject.transform, true);
            // Рекурсивно создаем кнопки для дочерних объектов
            BuildHierarchy(rootObject.transform);
        }

        initialCameraPosition = mainCamera.transform.position;
        initialCameraRotation = mainCamera.transform.rotation;
    }

    void BuildHierarchy(Transform parent)
    {
        foreach (Transform child in parent)
        {
            CreateButton(child, false);
            BuildHierarchy(child);
        }
    }

    void CreateButton(Transform target, bool isRoot)
    {
        // Создаем кнопку
        GameObject buttonObj = Instantiate(buttonPrefab, contentParent);
        UnityEngine.UI.Button button = buttonObj.GetComponent<UnityEngine.UI.Button>();
        Text buttonText = buttonObj.GetComponentInChildren<Text>();

        // Устанавливаем текст кнопки
        buttonText.text = target.name;

        // Устанавливаем цвет кнопки для корневого объекта
        if (isRoot)
        {
            ColorBlock colorBlock = button.colors;
            colorBlock.normalColor = rootButtonColor;
            colorBlock.highlightedColor = new Color(
                Mathf.Clamp01(rootButtonColor.r + .5f),
                Mathf.Clamp01(rootButtonColor.g + .5f),
                Mathf.Clamp01(rootButtonColor.b + .5f),
                rootButtonColor.a
            );
            colorBlock.pressedColor = new Color(
                Mathf.Clamp01(rootButtonColor.r + 0.2f),
                Mathf.Clamp01(rootButtonColor.g + 0.2f),
                Mathf.Clamp01(rootButtonColor.b + 0.2f),
                rootButtonColor.a
            );
            colorBlock.selectedColor = new Color(
                Mathf.Clamp01(rootButtonColor.r + 0.2f),
                Mathf.Clamp01(rootButtonColor.g + 0.2f),
                Mathf.Clamp01(rootButtonColor.b + 0.2f),
                rootButtonColor.a
            );

            button.colors = colorBlock;
            buttonText.color = rootButtonTextColor;
        }
        else
        {
            ColorBlock colorBlock = button.colors;
            colorBlock.normalColor = normalButtonColor;
            colorBlock.highlightedColor = new Color(
                Mathf.Clamp01(normalButtonColor.r + .5f),
                Mathf.Clamp01(normalButtonColor.g + .5f),
                Mathf.Clamp01(normalButtonColor.b + .5f),
                normalButtonColor.a
            );
            colorBlock.pressedColor = new Color(
                Mathf.Clamp01(normalButtonColor.r + 0.2f),
                Mathf.Clamp01(normalButtonColor.g + 0.2f),
                Mathf.Clamp01(normalButtonColor.b + 0.2f),
                normalButtonColor.a
            );
            colorBlock.selectedColor = selectedButtonColor;

            button.colors = colorBlock;
            buttonText.color = rootButtonTextColor;
        }

        // Добавляем кнопку в словарь
        buttonDictionary[target] = buttonObj;

        // Добавляем обработчик нажатия
        button.onClick.AddListener(() => OnButtonClicked(target, isRoot));
    }

    public void OnButtonClicked(Transform target, bool isRoot)
    {
        foreach (Transform child in target)
        {
            if (buttonDictionary.ContainsKey(child))
            {
                buttonDictionary[child].SetActive(!buttonDictionary[child].activeSelf);
            }
        }

        if (isRoot)
        {
            ResetHierarchy(rootObject.transform);
            if (cameraCoroutine != null)
            {
                StopCoroutine(cameraCoroutine);
            }
            cameraCoroutine = StartCoroutine(SmoothMoveCamera(initialCameraPosition, initialCameraRotation));
            currentTarget = null;
            HighlightButton(null);
        }
        else
        {
            if (currentTarget == target)
            {
                currentTarget = null;
                if (cameraCoroutine != null)
                {
                    StopCoroutine(cameraCoroutine);
                }
                cameraCoroutine = StartCoroutine(SmoothMoveCamera(initialCameraPosition, initialCameraRotation));
                ResetHierarchy(rootObject.transform);
                HighlightButton(null);
            }
            else
            {
                currentTarget = target;
                DisableAllChildrenExcept(rootObject.transform, target);
                Vector3 targetPosition = target.position - target.forward * forwardCamera + target.up * upCamera;
                Quaternion targetRotation = Quaternion.LookRotation(target.position - targetPosition);
                if (cameraCoroutine != null)
                {
                    StopCoroutine(cameraCoroutine);
                }
                cameraCoroutine = StartCoroutine(SmoothMoveCamera(targetPosition, targetRotation));
                HighlightButton(target);
            }
        }
    }

    void DisableAllChildrenExcept(Transform parent, Transform exception)
    {
        foreach (Transform child in parent)
        {
            if (child != exception)
            {
                child.gameObject.SetActive(false);
            }
            else
            {
                child.gameObject.SetActive(true);
            }
        }
    }

    void ApplyObjectRotator(Transform target)
    {
        ObjectRotator rotator = mainCamera.GetComponent<ObjectRotator>();
        if (rotator != null)
        {
            rotator.target = target;
        }
    }

    void SaveOriginalTransforms(Transform parent)
    {
        originalTransforms[parent] = new TransformData(parent.position, parent.rotation, parent.localScale);

        foreach (Transform child in parent)
        {
            SaveOriginalTransforms(child);
        }
    }

    void ResetHierarchy(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(true);
            ResetHierarchy(child);
        }

        foreach (var kvp in originalTransforms)
        {
            Transform obj = kvp.Key;
            TransformData data = kvp.Value;

            obj.position = data.position;
            obj.rotation = data.rotation;
            obj.localScale = data.localScale;
        }
    }

    IEnumerator SmoothMoveCamera(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        float elapsedTime = 0;

        while (elapsedTime < transitionDuration)
        {
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / transitionDuration);
            mainCamera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedTime / transitionDuration);
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;
    }

    void HighlightButton(Transform target)
    {
        int k = 0;
        foreach (var kvp in buttonDictionary)
        {
            k += 1;
            if (k>1)//родительский не перекрашиваем
            {
                UnityEngine.UI.Button button = kvp.Value.GetComponent<UnityEngine.UI.Button>();
                ColorBlock colorBlock = button.colors;
                colorBlock.normalColor = (kvp.Key == target) ? selectedButtonColor : normalButtonColor;
                button.colors = colorBlock;

            }
            
        }
    }
}

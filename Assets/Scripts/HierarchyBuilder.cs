using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

using System.Collections;
using static UnityEngine.GraphicsBuffer;

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
    private bool isExpanded = true;


    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;
    private Coroutine cameraCoroutine;
    private Transform currentTarget;


    void Start()
    {
        //initialCameraPos = mainCamera.transform.position.to ;
        if (rootObject != null && buttonPrefab != null && contentParent != null && mainCamera != null)
        {
            // Сохраняем исходные трансформы
            SaveOriginalTransforms(rootObject.transform);

            // Создаем кнопку для корневого объекта
            CreateButton(rootObject.transform, 0, true);
            // Рекурсивно создаем кнопки для дочерних объектов
            BuildHierarchy(rootObject.transform, 1);
        }

        initialCameraPosition = mainCamera.transform.position;
        initialCameraRotation = mainCamera.transform.rotation;  


    }

    void BuildHierarchy(Transform parent, int level)
    {
        foreach (Transform child in parent)
        {
            CreateButton(child, level, false);
            BuildHierarchy(child, level + 1);
        }
    }

    void CreateButton(Transform target, int level, bool isRoot)
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

            // Немного изменяем цвета для нажатых и выделенных состояний
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
        } else
        {
            ColorBlock colorBlock = button.colors;

            colorBlock.normalColor = normalButtonColor;

            // Немного изменяем цвета для нажатых и выделенных состояний
            colorBlock.highlightedColor = new Color(
                Mathf.Clamp01(normalButtonColor.r + .5f),
                Mathf.Clamp01(normalButtonColor.g + .5f),
                Mathf.Clamp01(normalButtonColor.b + .5f),
                rootButtonColor.a
            );
            colorBlock.pressedColor = new Color(
                Mathf.Clamp01(normalButtonColor.r + 0.2f),
                Mathf.Clamp01(normalButtonColor.g + 0.2f),
                Mathf.Clamp01(normalButtonColor.b + 0.2f),
                rootButtonColor.a
            );
            colorBlock.selectedColor = selectedButtonColor;

            button.colors = colorBlock;
            buttonText.color = rootButtonTextColor;
            
        }

        // Прячем дочерние кнопки
      /*  if (!isRoot)
        {
            buttonObj.SetActive(false);
        }*/

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
            // Логика для обработки нажатия на корневую кнопку
            Debug.Log("Clicked on root: " + target.name);
            ResetHierarchy(rootObject.transform);
            if (cameraCoroutine != null)
            {
                StopCoroutine(cameraCoroutine);
            }
            cameraCoroutine = StartCoroutine(SmoothMoveCamera(initialCameraPosition, initialCameraRotation));

            ApplyObjectRotator(target);
            currentTarget = null;

            /*
            ResetCamera();
            isExpanded = !isExpanded;
            // снимем прошлую подсветку
            HighlightButton(null);
            if (!isExpanded) //вернев камеру как было
            {
                mainCamera.transform.position = target.position - target.forward * 1f + target.up * 1f;
                mainCamera.transform.LookAt(target);
            }*/

        }
        else
        {
            // Логика для обработки нажатия на дочернюю кнопку
            Debug.Log("Clicked on: " + target.name);

            if (currentTarget == target)
            {
                // Если нажали на тот же элемент, возвращаем камеру обратно и отображаем все детали
                currentTarget = null;
                if (cameraCoroutine != null)
                {
                    StopCoroutine(cameraCoroutine);
                }
                cameraCoroutine = StartCoroutine(SmoothMoveCamera(initialCameraPosition, initialCameraRotation));
                ResetHierarchy(rootObject.transform);
                // Снимаем выделение в UI
                HighlightButton(null);
            }
            else
            {
                // Если нажали на новый элемент, фокусируемся на нем
                currentTarget = target;
                DisableAllChildrenExcept(rootObject.transform, target);
                Vector3 targetPosition = target.position - target.forward * forwardCamera + target.up * upCamera;
                Quaternion targetRotation = Quaternion.LookRotation(target.position - targetPosition);
                if (cameraCoroutine != null)
                {
                    StopCoroutine(cameraCoroutine);
                }
                cameraCoroutine = StartCoroutine(SmoothMoveCamera(targetPosition, targetRotation));
                // Выделяем кнопку в UI
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
        }
    }
    /*
    void ResetCamera()
    {
        mainCamera.transform.LookAt(rootObject.transform);
        ObjectRotator rotator = mainCamera.GetComponent<ObjectRotator>();
        if (rotator != null)
        {
            rotator.target = null;
        }
    }
    */
    /*
    void FocusCameraOnObject(Transform target)
    {

        mainCamera.transform.position = target.position - target.forward * forwardCamera + target.up * upCamera;
        mainCamera.transform.LookAt(target);
    }*/

    IEnumerator SmoothMoveCamera(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        float elapsedTime = 0;
        //mainCamera.transform.LookAt(rootObject.transform);

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
    /*
    void ExpandHierarchyInUI(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (buttonDictionary.ContainsKey(current))
            {
                buttonDictionary[current].SetActive(true);

                // Если это не корневой элемент, разворачиваем его родитель
                if (current.parent != null && buttonDictionary.ContainsKey(current.parent))
                {
                    buttonDictionary[current.parent].SetActive(true);
                }
            }
            current = current.parent;
        }
    }*/

    void HighlightButton(Transform target)
    {
        int k = 0;
        foreach (var kvp in buttonDictionary)
        {
            k += 1;
            if (k>1) //Корневую пропускаем
            {
                UnityEngine.UI.Button button = kvp.Value.GetComponent<UnityEngine.UI.Button>();
                if (kvp.Key == target) 
                {
                    ColorBlock colorBlock = button.colors;
                    colorBlock.normalColor = selectedButtonColor;
                    button.colors = colorBlock;
                }
                else
                {
                    ColorBlock colorBlock = button.colors;
                    colorBlock.normalColor = normalButtonColor;
                    button.colors = colorBlock;
                }
            }
            
        }
    }

}


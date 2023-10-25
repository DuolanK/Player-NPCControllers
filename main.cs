using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{   
    private CharacterController controller;
    private Vector3 dir;
    private Animator anim;
    [SerializeField] private int speed;
    [SerializeField] private int coins;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Text coinsText;
    [SerializeField] private GameObject PlayerModel;
    private int lineToMove = 3;
    private float lineDistance = 4;
    private bool isSliding;
    private bool isTurn;



    private List<NPCController> npcList = new List<NPCController>();

    // Start is called before the first frame update
    void Start()
    {   
                // Получить компонент CapsuleCollider
        anim = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();
        Time.timeScale = 1;

        // Устанавливаем начальную позицию ближе к центру экрана
        Vector3 startPosition = transform.position;
        startPosition.x = 0f; // Измените значение X на то, которое соответствует центру
        transform.position = startPosition;
    }

    private void Update()
    {
        if (SwipeController.swipeRight)
        {
            if (lineToMove < 6) // Проверка на максимальное значение (6 линий)
                lineToMove++;

        }

        if (SwipeController.swipeLeft)
        {
            if (lineToMove > 0) // Проверка на минимальное значение (0 линия)
                lineToMove--;
   
        }

        if (controller.isGrounded && !isSliding && !isTurn)
            anim.SetBool("run", true);
        else
            anim.SetBool("run", false);

        // Проверка на умирание на крайних линиях
        if (lineToMove == 0 || lineToMove == 6)
        {

            Die();
        }

        Vector3 targetPosition = transform.position.z * transform.forward + transform.position.y * transform.up;

        // Добавьте проверки для остальных линий
        if (lineToMove == 0)
            targetPosition += Vector3.left * lineDistance * 3;
        else if (lineToMove == 1)
            targetPosition += Vector3.left * lineDistance * 2;
        else if (lineToMove == 2)
            targetPosition += Vector3.left * lineDistance;
        else if (lineToMove == 4)
            targetPosition += Vector3.right * lineDistance;
        else if (lineToMove == 5)
            targetPosition += Vector3.right * lineDistance * 2;
        else if (lineToMove == 6)
            targetPosition += Vector3.right * lineDistance * 3;

        if (transform.position == targetPosition)
            return;

        Vector3 diff = targetPosition - transform.position;
        Vector3 moveDir = diff.normalized * 25 * Time.deltaTime;

        
        if (moveDir.sqrMagnitude < diff.sqrMagnitude)
            controller.Move(moveDir);
        else
            controller.Move(diff);
        UpdateNPCPositionAndDirection();
    }

    private void UpdateNPCPositionAndDirection()
    {
        foreach (NPCController npc in npcList)
        {
            if (npc != null) // Проверка на наличие NPC в списке
            {
                // Определите направление от NPC к игроку
                Vector3 playerDirection = transform.position - npc.transform.position;
                playerDirection.y = 0f; // Убедитесь, что направление параллельно земле

                // Используйте метод LookAt, чтобы NPC смотрел в сторону игрока
                npc.transform.LookAt(transform.position);

                // Вычислите новую позицию NPC на основе направления и расстояния
                Vector3 newPosition = transform.position + playerDirection.normalized * 2f; // Замените 2f на нужное расстояние

                // Установите новую позицию NPC
                npc.transform.position = newPosition;
            }
        }
    }

    private void Die()
    {
        // Здесь вы можете выполнить действия, связанные с умиранием персонажа, например, показать панель с сообщением о проигрыше
        StartCoroutine(DieCoroutine());
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        dir.z = speed;
        controller.Move(dir * Time.fixedDeltaTime);
    }

    private IEnumerator DieCoroutine()
    {
        // Воспроизвести анимацию смерти, если есть
        if (anim != null)
        {
            anim.SetBool("die", true);
        }

        // Ждать некоторое время перед уничтожением объекта
        yield return new WaitForSeconds(2f); // Здесь 2f - это задержка в секундах, которую вы можете настроить под свои потребности

        if (anim != null)
        {
            anim.enabled = false;
        }
        


        Destroy(gameObject.GetComponent<CapsuleCollider>()); 
        // Уничтожить модель игрока
        Destroy(PlayerModel);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.tag == "obstacle")
        {   
            losePanel.SetActive(true);
            Time.timeScale = 1;
            StartCoroutine(DieCoroutine());
        }
                // Проверяем, столкновение с объектом NPC
        if (hit.gameObject.CompareTag("npc"))
        {
            // Получаем компонент NPC
            NPCController npc = hit.gameObject.GetComponent<NPCController>();

            // Проверяем, не собран ли уже этот NPC
            if (!npc.IsCollected)
            {
                // Вызываем метод сбора NPC
                CollectNPC(npc);
                Debug.Log(npcList);

                // Здесь можно добавить звук или анимацию сбора NPC
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Coin")
        {
            coins++;
            coinsText.text = coins.ToString();
            Destroy(other.gameObject);
        }
    }

    private void CollectNPC(NPCController npc)
    {
        // Добавляем NPC в список стада
        npcList.Add(npc);

        // Можно также изменить родителя объекта NPC, чтобы он следовал за персонажем
        npc.transform.parent = transform;

        // Здесь вы можете настроить логику, связанную с сбором NPC
    }
    private void RemoveNPC(NPCController npc)
    {
        // Удаляем NPC из списка стада
        npcList.Remove(npc);

        // Здесь можно настроить логику удаления NPC, например, уничтожение объекта NPC
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwipeController : MonoBehaviour
{
    public static bool tap, swipeLeft, swipeRight, swipeUp, swipeDown;
    private bool isDraging = false;
    private Vector2 startTouch, swipeDelta;

    private void Update()
    {
        tap = swipeDown = swipeUp = swipeLeft = swipeRight = false;
        #region ПК-версия
        if (Input.GetMouseButtonDown(0))
        {
            tap = true;
            isDraging = true;
            startTouch = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDraging = false;
            Reset();
        }
        #endregion

        #region Мобильная версия
        if (Input.touches.Length > 0)
        {
            if (Input.touches[0].phase == TouchPhase.Began)
            {
                tap = true;
                isDraging = true;
                startTouch = Input.touches[0].position;
            }
            else if (Input.touches[0].phase == TouchPhase.Ended || Input.touches[0].phase == TouchPhase.Canceled)
            {
                isDraging = false;
                Reset();
            }
        }
        #endregion

        //Просчитать дистанцию
        swipeDelta = Vector2.zero;
        if (isDraging)
        {
            if (Input.touches.Length < 0)
                swipeDelta = Input.touches[0].position - startTouch;
            else if (Input.GetMouseButton(0))
                swipeDelta = (Vector2)Input.mousePosition - startTouch;
        }

        //Проверка на пройденность расстояния
        if (swipeDelta.magnitude > 100)
        {
            //Определение направления
            float x = swipeDelta.x;
            float y = swipeDelta.y;
            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                
                if (x < 0)
                    swipeLeft = true;
                else
                    swipeRight = true;
            }
            else
            {
                
                if (y < 0)
                    swipeDown = true;
                else
                    swipeUp = true;
            }

            Reset();
        }

    }

    private void Reset()
    {
        startTouch = swipeDelta = Vector2.zero;
        isDraging = false;
    }
}



using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    private bool isCollected = false;
    private CharacterController controller;
    private Animator anim;
    private List<NPCController> npcList = new List<NPCController>();
    private int lineToMove = 3;
    private float lineDistance = 4;
    private Vector3 previousCharacterPosition;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float smoothness = 5f; // Новая переменная для плавного перемещения
    [SerializeField] private float minDistanceToPlayer = 2f; //min distance

    // Добавьте поле для ссылки на основного персонажа
    public Transform mainCharacter;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();

        // Найдите основного персонажа и сохраните его Transform
        mainCharacter = GameObject.FindWithTag("Player").transform;
    }

    private void Update()
    {
        // Проверяем, есть ли основной персонаж и изменилась ли его позиция
        if (mainCharacter != null && mainCharacter.position != previousCharacterPosition)
        {
            transform.position = mainCharacter.position;
            transform.rotation = mainCharacter.rotation;

            // Вычисляем направление движения основного персонажа
            Vector3 movementDirection = mainCharacter.position - previousCharacterPosition;
            movementDirection.y = 0; // Убираем вертикальную составляющую

                        // Вычисляем расстояние между NPC и игроком
            float distanceToPlayer = Vector3.Distance(transform.position, mainCharacter.position);

            // Если расстояние меньше минимального, корректируем позицию
            if (distanceToPlayer < minDistanceToPlayer)
            {
                // Вычисляем направление от NPC к игроку
                Vector3 playerDirection = mainCharacter.position - transform.position;
                playerDirection.y = 0f; // Убеждаемся, что направление параллельно земле

                // Корректируем позицию NPC, чтобы она была на минимальном расстоянии
                Vector3 newPosition = mainCharacter.position + playerDirection.normalized * minDistanceToPlayer;
                transform.position = newPosition;
            }

            // Применяем это направление движения к NPC с использованием плавного перемещения
            Vector3 targetPosition = mainCharacter.position + movementDirection.normalized * lineDistance;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothness);

            // Обновляем позицию NPC, чтобы не было поперечных смещений
            transform.position = mainCharacter.position;

            // Сохраняем текущую позицию основного персонажа
            previousCharacterPosition = mainCharacter.position;
        }

        // Обработка свайпов
        if (SwipeController.swipeRight)
        {
            if (lineToMove < 6) // Проверка на максимальное значение (6 линий)
                lineToMove++;

            // Изменяем направление на право
            Vector3 moveRight = Vector3.right * lineDistance;
            controller.Move(moveRight);
        }

        if (SwipeController.swipeLeft)
        {
            if (lineToMove > 0) // Проверка на минимальное значение (0 линия)
                lineToMove--;

            // Изменяем направление на лево
            Vector3 moveLeft = Vector3.left * lineDistance;
            controller.Move(moveLeft);
        }
    }
    


    
    public interface ICollectable
    {
        void Collect();
    }

    // Метод для сбора NPC
    public void CollectNPC()
    {
        // Отмечаем NPC как собранного
        isCollected = true;

        // Останавливаем движение NPC
        // Здесь можно также включить анимацию сбора NPC

        // Убираем NPC из родителя, если он был прикреплен к персонажу
        transform.parent = null;
    }

    // Метод для проверки, собран ли NPC
    public bool IsCollected
    {
        get { return isCollected; }
    }

    private void Die()
    {
        // Здесь вы можете выполнить действия, связанные с умиранием персонажа, например, показать панель с сообщением о проигрыше
        anim.SetBool("npc_die", true);
        
    }

    private void FixedUpdate()
    {
        // Ваша логика для FixedUpdate
    }


    private IEnumerator DieCoroutine()
    {
        // Воспроизвести анимацию смерти, если есть
        anim.SetBool("npc_die", true);

        // Ждать некоторое время перед уничтожением объекта
        yield return new WaitForSeconds(2f); // Здесь 2f - это задержка в секундах, которую вы можете настроить под свои потребности

        // Уничтожить объект NPC
        Destroy(gameObject);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.tag == "obstacle")
        {   
            StartCoroutine(DieCoroutine());
        }

    }

    private void OnTriggerExit(Collider other)
    {
        // Ваша логика при выходе из коллайдера
    }
}



















using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwipeController : MonoBehaviour
{
    bool isDragging, isMobilePlatform;
    Vector2 tapPoint, swipeDelta;
    float minSwipeDelta = 130;

    public enum SwipeType
    {
        LEFT,
        RIGHT,
        UP,
        DOWN,
    }

    public delegate void OnSwipeInput(SwipeType type);
    public static event OnSwipeInput SwipeEvent;

    private void Awake()
    {
        #if UNITY_EDITOR || UNITY_STANDALONE
            isMobilePlatform = false;
        #else
            isMobilePlatform = true;
        #endif        
    }

    private void Update()
    {
        if (!isMobilePlatform)
        {
            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                tapPoint = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
                ResetSwipe();
        }
        else
        {
            if (Input.touchCount > 0)
            {
                if (Input.touches[0].phase == TouchPhase.Began)
                {
                    isDragging = true;
                    tapPoint = Input.touches[0].position;
                }
                else if (Input.touches[0].phase == TouchPhase.Canceled ||
                         Input.touches[0].phase == TouchPhase.Ended)
                    ResetSwipe();     
            }
        }

        CalculateSwipe();
    }

    void CalculateSwipe()
    {
        swipeDelta = Vector2.zero;

        if (isDragging)
        {
            if (!isMobilePlatform && Input.GetMouseButton(0))
                swipeDelta = (Vector2)Input.mousePosition - tapPoint;
            else if (Input.touchCount > 0) // Change "Input.touchDown" to "Input.touchCount"
                swipeDelta = Input.touches[0].position - tapPoint;
        }

        if (swipeDelta.magnitude > minSwipeDelta)
        {
            if (SwipeEvent != null) // Change "!-" to "!="
            {
                if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
                    SwipeEvent(swipeDelta.x < 0 ? SwipeType.LEFT : SwipeType.RIGHT); // Change "SwipeType.Right" to "SwipeType.RIGHT"
                else
                    SwipeEvent(swipeDelta.y > 0 ? SwipeType.UP : SwipeType.DOWN); // Change "SwipeType.Down" to "SwipeType.DOWN"
            }
            ResetSwipe();
        }
    }

    void ResetSwipe()
    {
        isDragging = false;
        tapPoint = swipeDelta = Vector2.zero;
    }
}    












using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{   
    private CharacterController controller;
    Vector3 moveVec;
    private Animator anim;
    [SerializeField] private int coins;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Text coinsText;
    [SerializeField] private GameObject PlayerModel;
    [SerializeField] private float jumpForce;
    [SerializeField] private float gravity;
    private int lineToMove = 3;
    private float lineDistance = 4;
    private bool isSliding;
    private List<NPCController> npcList = new List<NPCController>();
    public int liveNPCCount;
    private bool isAlive = false;
    private bool gameStarted = false;
    public AudioSource audioSource; // Ссылка на компонент AudioSource

    public float speed = 10, jumspeed = 12;

    int laneNumber = 1,
        lanesCount = 8;

    public float FirstLanePos,
                 LaneDistance,
                 SideSpeed;    


    // Start is called before the first frame update
    void Start()
    {   
                // Получить компонент CapsuleCollider
        anim = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();
        Time.timeScale = 1;

        // // Устанавливаем начальную позицию ближе к центру экрана
        // Vector3 startPosition = transform.position;
        // startPosition.x = 1f; // Измените значение X на то, которое соответствует центру
        // transform.position = startPosition;
        gameStarted = false;
        audioSource = GetComponent<AudioSource>();
        SwipeController.SwipeEvent += CheckInput;
        moveVec = new Vector3(0, 0, speed);
    }

    private void Update()
    {   
        if (!gameStarted)
        {
            // Проверяем первый свайп вправо
            if (laneNumber>1)
            {
                // Включаем движение после первого свайпа вправо
                StartCoroutine(GoStartCoroutine());
                Camera.main.GetComponent<CameraController>().isFixedUpdateEnabled = true; 
            }
        }
        moveVec.z = speed;
        controller.Move(moveVec * Time.fixedDeltaTime);
        Vector3 newPos = transform.position;
        newPos.x = Mathf.Lerp(newPos.x, FirstLanePos + (laneNumber * LaneDistance), Time.deltaTime * SideSpeed);
        transform.position = newPos;

        
    }

    void CheckInput(SwipeController.SwipeType type)
    {
        // if (isGrounded() && !isRolling)
        // {
        //     if (input.GetAxisRaw("Vertical") <0)
        //      StartCoroutine(DoRoll());
        // }

        int sign = 0;

        if (type == SwipeController.SwipeType.LEFT)
            sign = -1;
        else if (type == SwipeController.SwipeType.RIGHT)
            sign = 1;
        else 
            return;

        laneNumber += sign;
        laneNumber = Mathf.Clamp(laneNumber, 0, lanesCount); 
        if (laneNumber > 1)
        {
            Debug.Log("Right");
        }
        if (laneNumber < 1)
        {
            Debug.Log("LEFT");
        }       
    }

    bool isGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 0.05f);
    }


    private void Jump()
    {
        // dir.y = jumpForce;
    }

    // Update is called once per frame
    void FixedUpdate()
    {   
        // if (gameStarted)
        // {
        // dir.y += gravity * Time.fixedDeltaTime;
        // controller.Move(dir * Time.fixedDeltaTime);
        // }
    }


    private IEnumerator GoStartCoroutine()
    {
        // Ждать некоторое время
        yield return new WaitForSeconds(0.01f); // Здесь 2f - это задержка в секундах, которую вы можете настроить под свои потребности
        gameStarted = true;
    }




    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
                // Проверяем, столкновение с объектом NPC
        if (hit.gameObject.CompareTag("npc"))
        {
            // Получаем компонент NPC
            NPCController npc = hit.gameObject.GetComponent<NPCController>();

            // Проверяем, не собран ли уже этот NPC
            if (!npc.IsCollected)
            {
                // Вызываем метод сбора NPC
                npc.CollectNPC();
                liveNPCCount++;
            }
        }
    }

    public void NPCdeath()
    {
        liveNPCCount--;
        if (isAlive == false && liveNPCCount == 0)
            {
                losePanel.SetActive(true);
                Camera.main.GetComponent<CameraController>().Camerastop();
            }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Coin"))
        {
            OnCoinCollected();

            // Уничтожить монетку
            Destroy(other.gameObject);
            audioSource.Play();

        }
        if (other.gameObject.CompareTag("finish"))
        {
            Camera.main.GetComponent<CameraController>().MoveCameraInSemicircle();
        }
        if (other.gameObject.CompareTag("realfinish"))
        {
            // Включить анимацию twist
            if (anim != null)
            {
                anim.SetBool("twist", true);
            }

            // Остановить игрока
            // speed = 0;

            NPCController npc = FindObjectOfType<NPCController>(); // Получаем компонент NPCController из текущего объекта
            if (npc != null) // Проверяем, что компонент был успешно найден
            {   
                npc.Twist(); // Вызываем метод Twist()
            }
            winPanel.SetActive(true); 
        }
        if (other.gameObject.CompareTag("down"))
        {   
            if (controller.isGrounded)
                Jump();
        }
        if (other.gameObject.CompareTag("camera_stop"))
        {   

            Camera.main.GetComponent<CameraController>().Camerastop();
        }

    }



    public void OnCoinCollected()
    {   
        coins++;
        coinsText.text = coins.ToString();
        
    }
    
    private void CollectNPC(NPCController npc)
    {
        // Добавляем NPC в список стада
        npcList.Add(npc);

        // Можно также изменить родителя объекта NPC, чтобы он следовал за персонажем
        //npc.transform.parent = transform;

        // Здесь вы можете настроить логику, связанную с сбором NPC
    }
    private void RemoveNPC(NPCController npc)
    {
        // Удаляем NPC из списка стада
        npcList.Remove(npc);

        // Здесь можно настроить логику удаления NPC, например, уничтожение объекта NPC
    }

    public void StopAllNPCs()
    {
        foreach (var npc in npcList)
        {
            npc.Twist();
        }
    }
}








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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CooldownItem : MonoBehaviour
{
    [Header("Objects")]
    public GameObject sliderGameObject;
    public GameObject cooldownTextObject;

    private Text cooldownTextComponent;
    private Image imageComponent;
    private Slider sliderComponent;
    private RectTransform rectTransform;
    private MenuScript menuScript;
    private Enums.CooldownType type;

    //
    private float actualCooldown, initialCooldown;
    private bool initialized = false;

    // Start is called before the first frame update
    public void Initialize(Sprite image, float cooldown, Vector3 position, MenuScript script, Enums.CooldownType cooldownType)
    {
        cooldownTextComponent = cooldownTextObject.GetComponent<Text>();
        imageComponent = this.GetComponent<Image>();
        sliderComponent = sliderGameObject.GetComponent<Slider>();
        rectTransform = this.GetComponent<RectTransform>();
        menuScript = script;
        type = cooldownType;
        

        imageComponent.sprite = image;
        actualCooldown = cooldown;
        initialCooldown = cooldown;
        rectTransform.position = position;
        initialized = true;
    }

    public void UpdateCooldown(float cooldown)
    {
        initialCooldown = cooldown;
        actualCooldown = cooldown;
    }
    public void UpdateCooldown(Vector3 position)
    {
        rectTransform.position = position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialized)
            return;

        // if the cooldown it's done, call the menu script to delete it
        if (actualCooldown < 0)
            menuScript.RemoveCooldown(type);

        actualCooldown -= Time.deltaTime;
        sliderComponent.value = actualCooldown / initialCooldown;
        cooldownTextComponent.text = Mathf.Round(actualCooldown).ToString();
    }
}

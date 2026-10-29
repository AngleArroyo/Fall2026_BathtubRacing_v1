//https://www.youtube.com/watch?v=QGPpvOXtgG8 based off of this video tutorial
using Org.BouncyCastle.Security;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class TextMeshPro_Hyperlinks : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler, IMoveHandler, ISubmitHandler
{
    [Header("Animation Settings for Highlighting on Controller")]
    [SerializeField] private Color color = Color.black;
    [SerializeField] private float scaleStrength = 1.2f;
    [SerializeField] private float pulseStrength = 0.08f;
    [SerializeField] private float pulseSpeed = 4f;

    private TMP_Text textData;
    private int selectedLinkIndex = -1;

     void Awake()
    {
        textData = GetComponent<TMP_Text>();
    }

    void LateUpdate()
    {
        AnimateSelectedHyperlink();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(textData, eventData.position, null);
        if (linkIndex != -1)
        {
            OpenLink(linkIndex);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (textData.textInfo.linkCount > 0)
        {
            selectedLinkIndex = 0;
            Debug.Log("Link clicked");
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selectedLinkIndex = -1;
    }

    public void OnMove(AxisEventData eventData)
    {
        if (textData.textInfo.linkCount <= 0)
            return;

        if (eventData.moveDir == MoveDirection.Right)
        {
            selectedLinkIndex++;

            if (selectedLinkIndex >= textData.textInfo.linkCount)
                selectedLinkIndex = 0;
            Debug.Log("Linked selected");
            eventData.Use();
        }
        else if (eventData.moveDir == MoveDirection.Left)
        {
            selectedLinkIndex--;

            if (selectedLinkIndex < 0)
            {
                selectedLinkIndex = textData.textInfo.linkCount - 1;
            }
            Debug.Log("Linked selected");
            eventData.Use();
        }
        else if (eventData.moveDir == MoveDirection.Up)
        {
            Selectable select = GetComponent<Selectable>();
            if (select != null)
            {
                Selectable next = select.FindSelectableOnUp();

                if (next != null)
                {
                    EventSystem.current.SetSelectedGameObject(next.gameObject);
                }
            }
        }
        else if (eventData.moveDir == MoveDirection.Down)
        {
            Selectable select = GetComponent<Selectable>();

            if (select != null)
            {
                Selectable next = select.FindSelectableOnDown();

                if (next != null)
                {
                    EventSystem.current.SetSelectedGameObject(next.gameObject);
                }
            }
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (selectedLinkIndex >= 0 && selectedLinkIndex < textData.textInfo.linkCount)
            OpenLink(selectedLinkIndex);
    }

    private void OpenLink(int linkIndex)
    {
        TMP_LinkInfo link = textData.textInfo.linkInfo[linkIndex];

        Application.OpenURL(link.GetLinkID());
    }

    private void AnimateSelectedHyperlink()
    {
        textData.ForceMeshUpdate();

        if (selectedLinkIndex < 0 || selectedLinkIndex >= textData.textInfo.linkCount)
            return;

        TMP_LinkInfo linkInfo = textData.textInfo.linkInfo[selectedLinkIndex];
        int firstCharacter = linkInfo.linkTextfirstCharacterIndex;
        int charCount = linkInfo.linkTextLength;

        //animations below
        float pulse = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseStrength;
        float currentScale = scaleStrength + pulse;

        for (int i = 0; i < charCount; i++)
        {
            int charIndex = firstCharacter + i;

            if (charIndex >= textData.textInfo.characterCount)
                continue;

            TMP_CharacterInfo charInfo = textData.textInfo.characterInfo[charIndex];

            if (!charInfo.isVisible)
                continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] vertex = textData.textInfo.meshInfo[materialIndex].vertices;
            Color32[] colors = textData.textInfo.meshInfo[materialIndex].colors32;

            Vector3 center = (vertex[vertexIndex] + vertex[vertexIndex + 2]) / 2f;

            for (int j = 0; j < 4; j++)
            {
                int index = vertexIndex + j;

                vertex[index] = center + (vertex[index] - center) * currentScale;

                colors[index] = color;
            }

        }
        textData.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);

    }
    /*public void OnPointerClick(PointerEventData eventData)
        {
            TMP_Text textData = GetComponent<TMP_Text>();

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(textData, eventData.position, null);

            if (linkIndex != -1)
            {
                TMP_LinkInfo link = textData.textInfo.linkInfo[linkIndex];
                Application.OpenURL(link.GetLinkID());
            }
            else
            {
                Debug.Log("This is not working");
            }
        }*/
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BasicPuzzle.Core;
using BasicPuzzle.Data;

namespace BasicPuzzle.UI
{
    public class SkillManager : MonoBehaviour
    {
        [Header("Skill Buttons")]
        [SerializeField] private Button slowMoButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button trapShieldButton;

        [Header("Button Text Overlays (Optional)")]
        [SerializeField] private TextMeshProUGUI slowMoText;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private TextMeshProUGUI trapShieldText;

        [Header("Colors")]
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color usedColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        private void Start()
        {
            // Subscribe to GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelLoaded += HandleLevelLoaded;
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                GameManager.Instance.OnSkillsUpdated += RefreshButtons;
            }

            // Assign button click listeners
            if (slowMoButton != null)
                slowMoButton.onClick.AddListener(OnSlowMoClicked);
            if (hintButton != null)
                hintButton.onClick.AddListener(OnHintClicked);
            if (trapShieldButton != null)
                trapShieldButton.onClick.AddListener(OnTrapShieldClicked);

            RefreshButtons();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelLoaded -= HandleLevelLoaded;
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnSkillsUpdated -= RefreshButtons;
            }
        }

        private void HandleLevelLoaded(LevelData levelData)
        {
            RefreshButtons();
        }

        private void HandleStateChanged(GameState state)
        {
            RefreshButtons();
        }

        public void RefreshButtons()
        {
            if (GameManager.Instance == null) return;

            int currentLevel = GameManager.Instance.CurrentLevelIndex;
            GameState state = GameManager.Instance.CurrentState;

            // 1. Slow Motion (Unlocks at Level 5)
            bool isSlowMoUnlocked = currentLevel >= 5;
            bool isSlowMoUsed = GameManager.Instance.IsSlowMoUsedInCurrentLevel;
            // Can be used during ShowingWords, Shuffling, Gameplay
            bool isSlowMoStateValid = state == GameState.ShowingWords || state == GameState.Shuffling || state == GameState.Gameplay;
            UpdateButtonState(slowMoButton, slowMoText, isSlowMoUnlocked, isSlowMoUsed, isSlowMoStateValid, "Lvl 5");

            // 2. Hint (Unlocks at Level 12)
            bool isHintUnlocked = currentLevel >= 12;
            bool isHintUsed = GameManager.Instance.IsHintUsedInCurrentLevel;
            // Can only be used during Gameplay (guessing)
            bool isHintStateValid = state == GameState.Gameplay;
            UpdateButtonState(hintButton, hintText, isHintUnlocked, isHintUsed, isHintStateValid, "Lvl 12");

            // 3. Trap Shield (Unlocks at Level 18)
            bool isShieldUnlocked = currentLevel >= 18;
            bool isShieldUsed = GameManager.Instance.IsShieldUsedInCurrentLevel;
            // Can only be used during Gameplay (guessing)
            bool isShieldStateValid = state == GameState.Gameplay;
            UpdateButtonState(trapShieldButton, trapShieldText, isShieldUnlocked, isShieldUsed, isShieldStateValid, "Lvl 18");
        }

        private void UpdateButtonState(Button button, TextMeshProUGUI textOverlay, bool isUnlocked, bool isUsed, bool isStateValid, string unlockLevelName)
        {
            if (button == null) return;

            Image btnImg = button.GetComponent<Image>();

            if (!isUnlocked)
            {
                button.interactable = false;
                if (btnImg != null) btnImg.color = lockedColor;
                if (textOverlay != null)
                {
                    textOverlay.gameObject.SetActive(true);
                    textOverlay.text = unlockLevelName;
                }
            }
            else if (isUsed)
            {
                button.interactable = false;
                if (btnImg != null) btnImg.color = usedColor;
                if (textOverlay != null)
                {
                    textOverlay.gameObject.SetActive(true);
                    textOverlay.text = "USED";
                }
            }
            else if (!isStateValid)
            {
                button.interactable = false;
                if (btnImg != null) btnImg.color = lockedColor;
                if (textOverlay != null)
                {
                    textOverlay.gameObject.SetActive(false);
                }
            }
            else
            {
                button.interactable = true;
                if (btnImg != null) btnImg.color = activeColor;
                if (textOverlay != null)
                {
                    textOverlay.gameObject.SetActive(false);
                }
            }
        }

        private void OnSlowMoClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UseSlowMotionSkill();
            }
        }

        private void OnHintClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UseHintSkill();
            }
        }

        private void OnTrapShieldClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UseTrapShieldSkill();
            }
        }
    }
}

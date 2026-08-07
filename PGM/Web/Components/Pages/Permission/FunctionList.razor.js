/** 捲動至編輯區並聚焦指定欄位（供新增／編輯後顯示表單）。 */
export function revealEditor(panel, focusId) {
  if (panel) {
    panel.scrollIntoView({ behavior: "smooth", block: "start" });
  }
  if (!focusId) {
    return;
  }
  const input = document.getElementById(focusId);
  if (input) {
    input.focus({ preventScroll: true });
  }
}

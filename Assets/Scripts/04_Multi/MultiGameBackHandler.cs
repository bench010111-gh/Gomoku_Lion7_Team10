using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

// 멀티 게임 씬에서 뒤로가기 버튼 클릭 시
// 게임 진행 중이면 먼저 패배 처리 후 Photon 방을 나가고,
// 게임 진행 중이 아니면 바로 방을 나간 뒤 멀티 로비 씬으로 이동한다.
public class MultiGameBackHandler : MonoBehaviourPunCallbacks
{
    public string targetSceneName = "05_MultiLobbyScene";

    private bool isLeaving = false;

    public void OnClickBack()
    {
        if (isLeaving)
            return;

        isLeaving = true;

        if (!PhotonNetwork.InRoom)
        {
            SceneManager.LoadScene(targetSceneName);
            return;
        }

        MultiGameManager gameManager = FindObjectOfType<MultiGameManager>();

        if (gameManager != null && gameManager.IsPlayingAsPlayer())
        {
            // 게임 중 나가기는 기권/패배로 처리
            gameManager.RequestLeaveRoomAsLoss();
            return;
        }

        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(targetSceneName);
    }
}
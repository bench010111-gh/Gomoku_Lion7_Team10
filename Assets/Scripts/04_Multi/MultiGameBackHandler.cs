using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

// 멀티 게임 씬에서 뒤로가기 버튼 클릭 시 현재 Photon 방을 나가고(연결 해제) 멀티 로비 씬으로 이동하도록 처리

public class MultiGameBackHandler : MonoBehaviourPunCallbacks
{
    public string targetSceneName = "05_MultiLobbyScene";

    public void OnClickBack()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(targetSceneName);
    }
}
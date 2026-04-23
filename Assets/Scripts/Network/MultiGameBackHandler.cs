using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

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
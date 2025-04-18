using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EzySlice; // Ezy-Slice フレームワークを利用するために必要

public class SliceObjects : MonoBehaviour
{
    // 切断面の色
    public Material MaterialAfterSlice;
    // 切断するレイヤー
    public LayerMask sliceMask;

    // オーバーラップボックスのサイズ
    private Vector3 overlapBoxSize = new Vector3(1, 0.01f, 1);

    void Update()
    {
        // Spaceキー押下時
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 切断処理を実行
            Cutting();
        }
    }

    // オブジェクト生成時にMeshColliderとRigidbodyをアタッチし、レイヤーを設定
    private void MakeItPhysical(GameObject obj)
    {
        obj.AddComponent<MeshCollider>().convex = true;
        Rigidbody rb = obj.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        obj.layer = LayerMask.NameToLayer("Sliceable"); // Sliceableレイヤーを適用
        obj.tag = "Sliceable"; // Sliceableタグを適用
    }

    public void Cutting()
    {
        // Planeのサイズを取得 (scaleが1,1,1の状態であればそのまま)
        Vector3 planeScale = transform.localScale;

        Collider[] objectsToSlice = Physics.OverlapBox(transform.position, overlapBoxSize, transform.rotation, sliceMask, QueryTriggerInteraction.Ignore);

        Debug.Log("検出されたオブジェクト数: " + objectsToSlice.Length);

        if (objectsToSlice.Length == 0)
        {
            Debug.LogWarning("OverlapBox にオブジェクトが検出されませんでした。");
            return;
        }

        foreach (Collider objectToSlice in objectsToSlice)
        {
            if (objectToSlice == null)
            {
                Debug.LogError("objectToSlice が null です！");
                continue;
            }

            Debug.Log("切断対象: " + objectToSlice.gameObject.name);

            GameObject[] slicedObjects = objectToSlice.gameObject.SliceInstantiate(transform.position, transform.up, MaterialAfterSlice);

            if (slicedObjects == null)
            {
                Debug.LogError("SliceInstantiate が null を返しました: " + objectToSlice.gameObject.name);
                continue;
            }

            if (slicedObjects.Length < 2)
            {
                Debug.LogError("SliceInstantiate の結果が不足しています！");
                continue;
            }

            MakeItPhysical(slicedObjects[0]);
            MakeItPhysical(slicedObjects[1]);

            slicedObjects[0].name = objectToSlice.gameObject.name + "_1";
            slicedObjects[1].name = objectToSlice.gameObject.name + "_2";

            //TofuVolumeをつける
            slicedObjects[0].AddComponent<TofuPiece>();
            slicedObjects[1].AddComponent<TofuPiece>();

            Destroy(objectToSlice.gameObject);
        }
    }
}

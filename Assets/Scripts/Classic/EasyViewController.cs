using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EasyViewController : MonoBehaviour {

	//控制摄像机的上下左右移动速率(该值可以通过Unity中Inspector面板进行修改)
	public float speed = 100;
	//控制摄像机视野放大和缩小的速率(该值可以通过Unity中Inspector面板进行修改)
	public float mouseSpeed = -10f;
 
	void Update () {
		//获取按下键盘的A、D键，也就是水平轴，值的范围（-1，1）
		float h = Input.GetAxis("Horizontal");
		//获取按下键盘的W、S键、也就是垂直轴，值的范围（-1，1）
		float v = Input.GetAxis("Vertical");
		//获取滚动鼠标滚轮的值，值得范围为（-1，1）
		float mouse = Input.GetAxis("Mouse ScrollWheel");
		//让摄像机进行上下左右的移动以及视野的放大和缩小
		//transform.Translate默认是按照自身的坐标系进行移动，所以我们通过添加Space.World参数让摄像机按照世界坐标系进行移动
		transform.Translate(new Vector3(h * speed,0, v * speed) * Time.deltaTime, Space.World);
        transform.gameObject.GetComponent<Camera>().orthographicSize+=mouseSpeed*mouse;
	}
}


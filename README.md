项目源自：https://github.com/forwolk/NavMeshExampleDOTS

功能：ecs 网格寻路

对原工程修改：升级为2020.2.b12。各种ecs包最新。允许随时重新导航，可能穿模,box 碰撞寻路报错等bug修正，允许多人同时寻路，效率优化

修改后：多人可高性能的寻路

未解决的问题:使用纯dots动态障碍寻路。暂时使用混合的方式，gameobject修改网格障碍数据，ecs寻路

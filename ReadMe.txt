Unity版本：2021.3.8f1
管线：URP12.1.7

使用方法：
1.将Settings文件夹配置好的Renderer Data添加进URP Asset，并将其设置成default
2.在Renderer Data中通过LTS DepthNormal中的Layer Mask手动配置想要描边的Layer
3.在Hierarchy面板中通过PostProcessing中的LTS Edge Detect组件调整描边样式
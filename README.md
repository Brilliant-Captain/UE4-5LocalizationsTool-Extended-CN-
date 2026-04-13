# UE4 本地化工具 v0.2.8

## 1. 工具简介

本工具用于查看、编辑、合并和处理 UE4 本地化相关文件，当前主要支持：

- `.locres`
- `.uasset`
- `.umap`

本次汉化与功能维护版本新增了多项 `Locres` 处理功能、哈希处理功能，以及更方便的查找与筛选功能。

原项目作者：

- `Amr Shaheen`

汉化版本新功能维护：

- `毕星索汉化组`

## 2. 打开文件

启动程序后，可通过以下方式打开文件：

- 点击左上角 `文件 -> 打开`
- 直接把文件拖拽到窗口中

支持打开：

- `Locres 文件`
- `Uasset 文件`
- `Umap 文件`

如果打开的是 `.locres` 文件，顶部会显示 `Locres 操作` 菜单。

## 3. 基础编辑功能

程序支持以下常用功能：

- 保存当前文件
- 导出全部文本
- 导入全部文本
- 查找
- 替换
- 筛选
- 撤销 / 重做
- 复制 / 粘贴
- 排序

## 4. 查找与替换

点击 `工具 -> 查找` 或使用快捷键 `Ctrl + F` 可打开查找栏。

当前查找栏支持：

- 搜索列选择
  - `Name`
  - `Text value`
- `区分大小写`
- `下一个`
- `上一个`
- `全部`

说明：

- 当搜索列选择为 `Name` 时，可以查找名称，但由于该列是只读列，不能直接替换。
- 当搜索列选择为 `Text value` 时，可以正常进行查找和替换。

## 5. 筛选功能

点击 `工具 -> 筛选` 可打开筛选窗口。

支持：

- 选择筛选列
- 区分大小写
- 正则表达式
- 反向模式
- 多个筛选值

筛选列一般建议使用：

- `Text value`
- `Name`

## 6. Locres 操作说明

打开 `.locres` 文件后，可以使用 `Locres 操作` 菜单。

### 6.1 编辑所选行

用于直接编辑当前选中的 Locres 条目。

### 6.2 删除所选行

删除当前选中的行。

### 6.3 新增行

手动新增一条新的 Locres 记录。

### 6.4 合并 Locres 文件

将多个 Locres 文件内容合并到当前文件中。

## 7. Locres 新增功能

### 7.1 追加旧 Locres 独有条目

功能逻辑：

- 读取旧 Locres 文件
- 遍历旧文件所有条目
- 如果旧文件中的某条 `Name` 在当前新文件中不存在
- 则把该条完整追加到当前文件末尾

会一起复制：

- `NameSpace`
- `NameSpace hash`
- `Key`
- `Key hash`
- `Value`
- `Value hash`

### 7.2 覆盖差异文本值

功能逻辑：

- 当旧文件与当前文件存在同名条目
- 且 `text value` 不同
- 可使用此功能将旧文件文本值覆盖到当前文件

执行时会弹出二次确认，并显示待覆盖数量。

覆盖后会同步更新：

- 当前条目的文本值
- `Value hash`

## 8. Locres 哈希功能

### 8.1 只覆盖哈希（文本一致）

适用场景：

- `Name` 相同
- `text value` 也相同
- 只想同步旧文件中的哈希值

执行前会弹出确认框，并显示符合条件数量。

### 8.2 只覆盖哈希（文本不同）

适用场景：

- `Name` 相同
- `text value` 不同
- 只想覆盖哈希，不改当前文本

注意：

- 此操作不会修改当前文本内容
- 但会写入旧文件中的文本哈希
- 因此执行后，当前文本与文本哈希可能不一致

执行前会弹出确认框，并显示数量。

### 8.3 只覆盖哈希（同名强制）

适用场景：

- 只要 `Name` 相同，就强制覆盖：
  - 命名空间哈希
  - 键哈希
  - 文本哈希

不要求文本一致。

执行前会弹出确认框，并显示数量。

### 8.4 重算所有哈希值

功能逻辑：

- 对当前文件所有条目重新计算：
  - `NameSpace hash`
  - `Key hash`
  - `Value hash`

规则：

- 如果命名空间为空，则 `NameSpace hash = 0`

适用场景：

- 你已经手动修改了文本
- 希望让哈希值与当前文件实际内容保持一致

## 9. 高亮颜色说明

当前程序会用不同颜色标记被处理过的数据：

- 橙色：`文本已改`
- 蓝色：`本次追加`
- 浅绿：`哈希覆盖`
- 浅青：`哈希重算`

说明：

- 文本被修改时，会优先保留文本修改高亮
- 追加条目会保留追加高亮
- 哈希类操作会对对应条目做独立高亮，便于区分处理来源

## 10. 建议使用流程

### 场景一：补回旧版本缺失条目

建议步骤：

1. 打开新的 `.locres`
2. 执行 `追加旧 Locres 独有条目`
3. 检查蓝色高亮条目
4. 保存

### 场景二：沿用旧版本翻译文本

建议步骤：

1. 打开新的 `.locres`
2. 执行 `覆盖差异文本值`
3. 检查橙色高亮条目
4. 保存

### 场景三：只同步哈希

根据需要选择：

- 文本一致时：`只覆盖哈希（文本一致）`
- 文本不同时：`只覆盖哈希（文本不同）`
- 不管文本是否一致：`只覆盖哈希（同名强制）`

### 场景四：文本改完后统一修正哈希

建议步骤：

1. 手动或批量修改文本
2. 执行 `重算所有哈希值`
3. 检查浅青色高亮条目
4. 保存

## 11. 命令行用法

### 11.1 导出单个文件

```bash
UE4localizationsTool.exe export <Locres/Uasset/Umap 文件路径> <选项>
```

示例：

```bash
UE4localizationsTool.exe export Actions.uasset
```

### 11.2 导入单个文件

```bash
UE4localizationsTool.exe import <txt 文件路径> <选项>
```

示例：

```bash
UE4localizationsTool.exe import Actions.uasset.txt
```

### 11.3 导入单个文件但不重命名

```bash
UE4localizationsTool.exe -import <txt 文件路径> <选项>
```

示例：

```bash
UE4localizationsTool.exe -import Actions.uasset.txt
```

### 11.4 批量导出文件夹内文件

```bash
UE4localizationsTool.exe exportall <文件夹> <输出文本文件> <选项>
```

示例：

```bash
UE4localizationsTool.exe exportall Actions text.txt
```

### 11.5 批量导入文件夹内文件

```bash
UE4localizationsTool.exe importall <文件夹> <文本文件> <选项>
```

示例：

```bash
UE4localizationsTool.exe importall Actions text.txt
```

### 11.6 批量导入但不重命名

```bash
UE4localizationsTool.exe -importall <文件夹> <文本文件> <选项>
```

示例：

```bash
UE4localizationsTool.exe -importall Actions text.txt
```

### 11.7 命令行选项

#### 使用 GUI 中最后一次筛选条件

仅在名称表场景中适用：

```bash
-f
-filter
```

示例：

```bash
UE4localizationsTool.exe export Actions.uasset -filter
```

#### 导出时不包含名称表

```bash
-nn
-NoName
```

示例：

```bash
UE4localizationsTool.exe export Actions.uasset -NoName
```

#### 使用 method2

用于在 `.uasset` 和 `.umap` 中尝试绕过常规 UE4 资源结构抓取文本：

```bash
-m2
-method2
```

示例：

```bash
UE4localizationsTool.exe export Actions.uasset -method2
UE4localizationsTool.exe export Actions.uasset -method2 -NoName -filter
```

## 12. 注意事项

- 在执行覆盖类操作前，建议先备份原文件
- `只覆盖哈希（文本不同）` 和 `只覆盖哈希（同名强制）` 都可能造成“当前文本”和“文本哈希”不一致，请按需求使用
- 如果你修改了文本内容，通常更推荐最后执行一次 `重算所有哈希值`
- 如果顶部内容显示较多，可以拖拽窗口边缘自由调整大小

## 13. 关于本版本

当前版本：

- `UE4 本地化工具 v0.2.8`

赞助链接：

- <https://qm.qq.com/q/8h4XCJyv16>

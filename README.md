SceneMeshUtils(仮)
====

## 概要

メッシュの一部をシーン上から直接選択するTriangle Selectorとそれを用いたユーティリティ。

## 機能一覧
- メッシュPrefabの生成(Create Module)
- マスク画像の生成(Create Mask Texture)
- メッシュ削除(Remove Mesh From Scene)
- 縮小BlendShapeの生成(Add Shrink BlendShape)
- ポリゴンの移動(Transform Polygon)

## 依存関係
- VRC.SDK3
- Non-Destructive Modular Framework

## Create Module

### 機能
- アバターや衣装の一部のみを取り出し、他のアバターに使用することを目的とした機能
- 欲しいメッシュの箇所を指定した上でウェイトやPhysBone等を走査し必要最低限の構成のPrefab Variantを生成

### 実行場所
- Skinned Mesh Rendererがついたオブジェクトの右クリックメニューから`SceneMeshUtils/Create Module`

### 使い方
- `Open Triangle Selector`からTriangle Selectorを起動
- 欲しい箇所のメッシュを指定した上でApply。詳細は以下の`Triangle Selector`から
- 選択された出力対象の箇所のメッシュがプレビューされます。
- `Create Module`もしくは`Create Both Module`
- 基本的にそのままMAセットアップ可能なメッシュやArmature等を内包したPrefabとそのインスタンスがAssetsとHierarchyにそれぞれ生成されます
- `Create Both Module`は選択したメッシュと選択されていないメッシュでそれぞれPrefabを生成します。独立した合計2つのメッシュに分離するような機能です。

## Create Mask Texture

### 機能
- マテリアルや他ツール等で使用できるメッシュの任意の箇所のマスク画像等を生成する機能
- メッシュの箇所を指定した上で、選択領域と非選択領域に対してそれぞれ塗りつぶしや色の転送等を行ったテクスチャを生成

### 実行場所
- Skinned Mesh Rendererがついたオブジェクトの右クリックメニューから`SceneMeshUtils/Create Mask Texture`

### 使い方
- `Open Triangle Selector`からTriangle Selectorを起動
- メッシュの箇所を指定した上でApply。詳細は以下の`Triangle Selector`から
- 選択された箇所のメッシュがプレビューされます。
- 選択した箇所と選択されていない箇所をそれぞれどのような色にするか選択します。
    - white: 白に塗りつぶし
    - black: 黒に塗りつぶし
    - alpha: 透過に塗りつぶし
    - original: 元のテクスチャから転送
    - grayscale: 元のテクスチャから輝度情報のみ転送
- Create Mask Texture
- テクスチャがAssetsに保存されます

## Remove Mesh From Scene

### 機能
- 非破壊でメッシュを削除する機能。
- 基本的にAAO Remove Meshと同じですがTriangle Selectorを用いることでシーン上から直接選択できるインターフェースの違いがあります。

### 実行場所
- Skinned Mesh RendererがついたオブジェクトにAdd Componentから`SceneMeshUtils/Remove Mesh From Scene`

### 使い方
- `Open Triangle Selector`からTriangle Selectorを起動
- 消したい箇所のメッシュを指定した上でApply。詳細は以下の`Triangle Selector`から
- Auto Previewが有効であれば削除されたメッシュがプレビューされます。
- NDMF準拠でビルド時にメッシュ削除

## Add Shrink BlendShape

### 機能
- 貫通対策用のBlendShapeを非破壊で追加する機能。
- Triangle Selectorでアイランド単位の選択を行った箇所に対してはメッシュの表示/非表示を行うアニメーションに転用できます。
- Blenderで追加するほど綺麗なShrinkにはなりません。

### 実行場所
- Skinned Mesh RendererがついたオブジェクトにAdd Componentから`SceneMeshUtils/Add Shrink BlendShape`

### 使い方
- `Open Triangle Selector`からTriangle Selectorを起動
- 消したい箇所のメッシュを指定した上でApply。詳細は以下の`Triangle Selector`から
- Auto Previewが有効であればBlendShapeが追加されたメッシュがプレビューされます。
- NDMF準拠でビルド時にBlendShapeを追加

## Transform Polygon(開発途中)

### 機能
- メッシュの一部にのみTransformを適用する機能。
- 通常メッシュの移動、回転、スケールはウェイトに従って動作しますが、そのウェイトが意図した通りに配置されていない場合、任意の箇所にのみ効果を適用できません。
- この機能は非破壊でMeshを直接編集することで選択された箇所の頂点に対してのみ追加のTransformの値を加算します。

### 実行場所
- Skinned Mesh RendererがついたオブジェクトにAdd Componentから`SceneMeshUtils/Transform Polygon`

### 使い方
- `Open Triangle Selector`からTriangle Selectorを起動
- 適用したい箇所のメッシュを指定した上でApply。詳細は以下の`Triangle Selector`から
- コンポーネント上にあるTransformに準拠した項目の数値を変更します
- Auto Previewが有効であれば選択されたメッシュのみPosition等が変化します。
- NDMF準拠でビルド時にMeshを編集

## Triangle Selector

### 機能
- メッシュの一部をシーン上から直接選択する共通のインターフェース

### 実行場所
- 各機能の`Open Triangle Selector`から起動できます。

### 使い方
- クリックもしくはドラッグを用いた範囲選択が出来ます。
- 選択された箇所のメッシュは起動時に開かれるSceneViewに移動されてプレビューされます。
- 構造的に分離されたメッシュの一部(アイランド)を選択するアイランドモードとポリゴンモードがあります。
- メッシュが選択できたらApplyボタンを押して適用します。

### 詳細説明

#### Language
- 英語と日本語を切り替えます  

#### 選択中 / 全ポリゴン
- 指定されたSkinned Mesh Rendererにつくメッシュの全ポリゴンと現在選択されているポリゴンのそれぞれの数。

#### すべて選択 / すべての選択を解除 / 選択を反転
- すべて選択は全ポリゴンを選択状態にします。
- すべての選択を解除は全ポリゴンを非選択状態にします。
- 選択を反転は現在選択されているポリゴンと非選択のポリゴンを反転します。

#### 元に戻す(Ctrl+Z) / やり直す(Ctrl+Y)
- 元に戻す(Undo)は選択領域を一つ前に戻します。Ctrl+Zからも操作できます。
- やり直す(Redo)は元に戻すを一つ打ち消す操作です。Ctrl+Yからも操作できます。

#### 選択モード
- アイランドモード
    - アイランドモードでは構造的に分離されたメッシュの一部(アイランド)が選択できます。
    - 通常選択ではクリックした箇所にあるアイランドが選択されます。
    - 範囲選択では範囲内に完全に入っているアイランドが選択されます。

    - メッシュをさらに分割
        - メッシュをさらに多くのアイランドに細かく分離します。無意味に小さな分離をすることがあるのでデフォルトではオフになっています。
    - 範囲内を全て選択
        - 範囲選択に関するオプションです。デフォルトでは、ドラッグされた範囲内に完全に含まれているアイランドのみが選択されます。これを、一部でも範囲内にあるアイランドも選択されるように変更します。

- ポリゴンモード
    - ポリゴンモードはポリゴンを直接選択できます。
    - 通常選択ではクリックした箇所から一定範囲内にあるポリゴンが選択されます。
    - 範囲選択では範囲内にあるポリゴンが全て選択されます。

    - スケール
        - 通常選択に関するオプションです。クリックした箇所からどれだけ離れた位置にあるポリゴンまで選択対象に含めるかの値を設定します。

#### 選択を中止/再開
- Triangle Selectorは起動中SceneViewに対し、Unity Editor標準の選択を無効化します。
- 選択を中止はこの動作を一時的に停止し標準の選択が行えるようにします。
- 選択を再開はTriangle Selector固有の選択を再開します。

#### 選択箇所の名前(オプション)
- 選択箇所を保存する際にその保存名を指定できます。
- 入力がない場合は自動で設定されます。

#### 適用
- 選択箇所を呼び出し元の機能に適用します。

### Tips

#### 範囲選択について
- Iso(Isometric/平行投影)とPersp(Perspective/透視投影)で効果が異なります。
- Isoが選択方向に大して直方体を構成する選択領域を持つのに対し、Perspは選択方向に対して広がる四角錐の選択領域を持ちます。
- これはPerspは選択方向に対して奥におくほど広い範囲のポリゴンが選択されることを意味します。
- そのため正確な範囲選択を行う場合などは基本的にIsoを用いることが推奨されます。
- 上記の問題に関連して、選択領域の奥行きはIsoで10mである一方、Perspでは3mに制限されています。

#### 選択領域の保持について
- Triangle Selectorで選択された範囲は全て保持されます。
- Open Triangle Selectorの上部にあるTriangle Selectionのドロップダウンからそのメッシュに対する過去の選択領域が選択できます。
- Assetsに保持されるため、コンポーネントやアバターを越えて同一のメッシュに対しては過去の選択を参照できます。
- Triangle Selectionのドロップダウンの横にあるEditからその選択領域に関する編集を行う新たな選択領域を追加できます。


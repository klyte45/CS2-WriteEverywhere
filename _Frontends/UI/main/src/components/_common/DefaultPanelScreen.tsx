import { Component } from "react";

type Props = {
    title: string;
    subtitle?: string;
    buttonsRowContent?: JSX.Element,
    isSubScreen?: boolean,
    offsets?: {
        bottom?: number,
        left?: number,
        right?: number,
        top?: number
    }
}

export class DefaultPanelScreen extends Component<Props, {}> {

    render() {
        return <>
            {this.props.isSubScreen ? <h2>{this.props.title}</h2> : <h1>{this.props.title}</h1>}
            {!this.props.isSubScreen && this.props.subtitle && <h3>{this.props.subtitle}</h3>}
            <section style={{
                overflow: "scroll", position: "absolute",
                bottom: (this.props.offsets?.bottom ?? 0) + (this.props.buttonsRowContent ? 52 : 5),
                left: this.props.offsets?.left ?? 5,
                right: this.props.offsets?.right ?? 5,
                top: this.props.offsets?.top ?? this.props.isSubScreen ? 60 : 107
            }}>
                {this.props.children}
            </section>
            {this.props.buttonsRowContent && <div style={{ display: "flex", position: "absolute", left: 5, right: 5, bottom: 5, flexDirection: "row-reverse" }}>
                {this.props.buttonsRowContent}
            </div>}
        </>
    }
}
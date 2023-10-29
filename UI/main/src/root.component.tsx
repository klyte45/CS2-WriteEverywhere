///<reference path="euis.d.ts" />

export default function Root(props) {
  return <>
    <h1>ON!</h1>
    <button onClick={() => location.reload()}>REFRESH PAGE</button>
  </>;
}

import { sidebar } from "vuepress-theme-hope";

export default sidebar({
  "/": [
    "",
    {
      text: "Tutorials",
      icon: "book",
      prefix: "guide/",
      children: "structure",
    },
  ],
});

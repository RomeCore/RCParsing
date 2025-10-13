import { sidebar } from "vuepress-theme-hope";

export default sidebar({
  "/": [
    "",
    "/getting_started.md",
    {
      text: "Tutorials",
      icon: "book",
      prefix: "guide/",
      children: "structure",
    },
    {
      text: "Elements library",
      prefix: "elements_library/",
      children: "structure",
    },
  ],
});

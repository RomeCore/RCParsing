import { defineUserConfig } from "vuepress";

import theme from "./theme.js";

export default defineUserConfig({
  base: "/",

  lang: "en-US",
  title: "RCParsing",
  description: "A docs for RCParsing",

  theme,

  // Enable it with pwa
  // shouldPrefetch: false,
});

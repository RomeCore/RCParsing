using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Benchmarks.GraphQL
{
	public static class TestInputs
	{
		public static string shortGraphQLQuery =
		"""
		query Search($term: String!, $limit: Int = 10) @cache {
		  search(term: $term, limit: $limit) {
			... on User {
			  id
			  name
			  email
			}
			... on Post {
			  id
			  title
			  content
			  author {
				name
			  }
			}
		  }
		}
		
		mutation BatchCreateUsers($inputs: [UserInput!]!) @transaction {
		  batchCreateUsers(inputs: $inputs) {
			id
			name
		  }
		}
		
		subscription OnUserUpdate($userId: ID!) {
		  userUpdated(userId: $userId) {
			id
			name
			...updatedFields
		  }
		}
		
		fragment updatedFields on User {
		  email
		  lastLogin
		}
		""";

		public static string bigGraphQLQuery =
		"""
		# FUCKING BENCHMARK FOR QUERY TESTING!!!
		# 200+ LINES OF QUERY! WHAT A SHIT!
		
		query UltraDeepNestedQuery {
		  # 1. MEGA DEEP NESTED QUERY
		  user(id: "12345") {
		    id
		    name
		    email
		    profile {
		      avatar
		      bio
		      location {
		        city
		        country
		        coordinates {
		          lat
		          lng
		        }
		      }
		      socialLinks {
		        twitter
		        github
		        linkedin
		        customLinks {
		          name
		          url
		          icon
		        }
		      }
		    }
		    friends(first: 50) {
		      edges {
		        node {
		          id
		          name
		          mutualFriends {
		            count
		            edges {
		              node {
		                id
		                name
		                friends {
		                  edges {
		                    node {
		                      id
		                      profile {
		                        bio
		                      }
		                    }
		                  }
		                }
		              }
		            }
		          }
		        }
		        cursor
		      }
		      pageInfo {
		        hasNextPage
		        endCursor
		      }
		    }
		    posts(filter: {type: PUBLIC, tags: ["tech", "graphql"]}) {
		      edges {
		        node {
		          id
		          title
		          content
		          createdAt
		          updatedAt
		          author {
		            id
		            name
		          }
		          comments(first: 100) {
		            edges {
		              node {
		                id
		                text
		                author {
		                  id
		                  name
		                  profile {
		                    avatar
		                  }
		                }
		                replies {
		                  edges {
		                    node {
		                      id
		                      text
		                      author {
		                        id
		                        name
		                      }
		                      likes {
		                        count
		                        users {
		                          id
		                          name
		                        }
		                      }
		                    }
		                  }
		                }
		                likes {
		                  count
		                }
		              }
		            }
		          }
		          tags {
		            id
		            name
		            relatedTags {
		              id
		              name
		            }
		          }
		          metadata {
		            views
		            likes
		            shares
		            readingTime
		          }
		        }
		      }
		    }
		  }
		
		  # 2. FRAGMENTS
		  ...UserDetails
		  ...PostDetails
		
		  # 3. INLINES FRAGMENTS
		  ... on AdminUser {
		    permissions {
		      canEdit
		      canDelete
		      canManageUsers
		      roles {
		        name
		        level
		        scopes
		      }
		    }
		    analytics {
		      totalUsers
		      activeUsers
		      popularPosts {
		        title
		        views
		      }
		    }
		  }
		
		  # 4. LOTS OF VARIABLES
		  featuredPosts: posts(
		    first: $count, 
		    sort: POPULAR, 
		    where: {
		      publishedAfter: $startDate,
		      tags: $selectedTags
		    }
		  ) @include(if: $showFeatured) @skip(if: $hidePosts) {
		    ...PostDetails
		    featuredUntil
		    promotion {
		      budget
		      targetAudience
		    }
		  }
		
		  # 5. HARD UNION TYPES
		  search(query: $searchQuery, types: [USER, POST, COMMENT]) {
		    ... on User {
		      id
		      name
		      profile {
		        bio
		      }
		    }
		    ... on Post {
		      id
		      title
		      excerpt
		    }
		    ... on Comment {
		      id
		      text
		      post {
		        title
		      }
		    }
		  }
		
		  # 6. INTERFACES WITH IMPLEMENTATIONS
		  notifications {
		    ... on LikeNotification {
		      id
		      actor {
		        name
		      }
		      post {
		        title
		      }
		      createdAt
		    }
		    ... on CommentNotification {
		      id
		      actor {
		        name
		      }
		      comment {
		        text
		      }
		      post {
		        title
		      }
		    }
		    ... on FollowNotification {
		      id
		      actor {
		        name
		        profile {
		          bio
		        }
		      }
		    }
		  }
		}
		
		# FRAGMENTS
		fragment UserDetails on User {
		  id
		  username
		  email
		  createdAt
		  updatedAt
		  settings {
		    theme
		    language
		    notifications {
		      email
		      push
		    }
		  }
		  statistics {
		    postCount
		    commentCount
		    likeCount
		    followerCount
		  }
		}
		
		fragment PostDetails on Post {
		  id
		  title
		  slug
		  content
		  excerpt
		  status
		  visibility
		  createdAt
		  updatedAt
		  publishedAt
		  author {
		    ...UserDetails
		  }
		  categories {
		    id
		    name
		    slug
		    parent {
		      id
		      name
		    }
		  }
		  seo {
		    title
		    description
		    keywords
		    openGraph {
		      image
		      type
		    }
		  }
		}
		
		# MUTATIONS
		mutation ComplexMutation($input: CreatePostInput!) {
		  createPost(input: $input) {
		    post {
		      ...PostDetails
		    }
		    errors {
		      field
		      message
		      code
		    }
		  }
		}
		
		# SUBSCRIBTIONS
		subscription RealTimeUpdates {
		  postUpdated(postId: $postId) {
		    ...PostDetails
		    liveStats {
		      concurrentViewers
		      liveReactions {
		        emoji
		        count
		        users {
		          id
		          name
		        }
		      }
		    }
		  }
		}
		
		# QUERY VARIABLES
		query DirectiveMadness {
		  users @include(if: $showUsers) @skip(if: $hideUsers) @deprecated(reason: "use newUsers") {
		    id
		    name @upperCase @truncate(length: 50)
		    email @mask @skip(if: $hideEmails)
		    posts @stream(initialCount: 10) {
		      id
		      title
		      content @transform(case: UPPERCASE)
		    }
		  }
		}
		
		# EXTREMELY DEEP QUERY
		query UltraDeep {
		  a1: node(id: "1") {
		    ... on TypeA {
		      b1: children {
		        ... on TypeB {
		          c1: nested {
		            ... on TypeC {
		              d1: deepField {
		                ... on TypeD {
		                  e1: deeper {
		                    ... on TypeE {
		                      f1: evenDeeper {
		                        ... on TypeF {
		                          g1: deepest {
		                            value
		                            children {
		                              h1: level8 {
		                                i1: level9 {
		                                  j1: level10 {
		                                    finalValue
		                                  }
		                                }
		                              }
		                            }
		                          }
		                        }
		                      }
		                    }
		                  }
		                }
		              }
		            }
		          }
		        }
		      }
		    }
		  }
		}
		
		# COMPLEX ARGUMENTS
		query ComplexArguments {
		  searchAdvanced(
		    filters: {
		      dateRange: {
		        from: "2024-01-01"
		        to: "2024-12-31"
		      }
		      priceRange: {
		        min: 100
		        max: 1000
		        currency: USD
		      }
		      location: {
		        near: {
		          lat: 40.7128
		          lng: -74.0060
		          radius: 50
		        }
		      }
		      tags: ["important", "urgent"]
		      status: [ACTIVE, PENDING]
		    }
		    pagination: {
		      page: 1
		      perPage: 100
		      sort: {field: CREATED_AT, order: DESC}
		    }
		  ) {
		    totalCount
		    results {
		      id
		      title
		      score
		    }
		  }
		}
		""";
	}
}
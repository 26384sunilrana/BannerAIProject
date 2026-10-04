1. The Shop owners will have to purchase subscription from me as Product Owner. 

   - a. This will be Quarterly, Half Yearly, Yearly. The cost will be equivalent. 

   - b. <mark>The Subscript</mark> i <mark>on will have Basic, Silver, Gold, Plat</mark> i <mark>num type.</mark> 

   - c. These Subscription will be on Auto renewal. 

   - d. If Shop owner decides not to go with Auto renewal. 

   - e. There will be reminder via SMS, Email to Renew the subscription. 

   - f. Since the cost of Subscription is Equivalent, User can change their subscription any time with immediate effect from next day. 

   - g. Then if Shop Owner forgets to renew, He/She will only be able to see a Default Digital Board that was created as static Banner for next 1 week. He/She will receive SMS and Email every day asking for Renewal. If He/She still misses the Opportunity. Then, the Login will be disabled and he will only be able to see the static Banner as it will be installed in his Local System. 

   - h. The Cost of the Subscription will be based on the Size of the Banner, equivalent Storage space. 

2. There will be Multiple Shop Owners associated with My Product. a. All shop Owners who have purchased my Subscription will be given a uniqueID. 

   - b. There will be Unique ID GROUP based on Location, Shops under that location will be mapped to this Unique ID. 

   - c. There will be Unique ID CITY based on City, GROUP under this City will be mapped to this Unique ID. 

   - d. There will be Unique ID STATE based on State, CITY under this STATE will be mapped to this Unique ID. 

   - e. There will be Unique ID COUNTRY based on Country, STAET under this Country will be mapped to this Unique ID. 

   - f. There will be option to perform CRUD operation on Shop, GROUP, CITY, STATE, COUNTRY. 

1. Each Shop owner will be able to perform below activity in my Product. a. Login: 

      - i. Every Shop owner, will have option to register or sign up to my Product. 

      - ii. Once the Owner has taken a Subscription, He will have option to create to more Logins or Sales Executive. 

      - iii. Shop Owner will also have option to Delete the Login of any Sales Executive. And create another Login. At any point there will only be 2 active Login under one Shop. 

         1. Shop Owner will have option to either keep himself as approver of Newly created Banner, or can make one or both as Approver of Newly Created banner. 

   2. In case of publish date change or any change in the Banner has to go through same approval state process as newly created banner. 

   3. In case the current banner date is change to future date, and there is no banner with current date, then Shop owner will see default banner from his local machine. 

- iv. Once registered, the shop owner will see all the offerings from me 

   1. Creation of components, dragging components on the Banner as expected in various shape and size. 

   2. Setting Index of each component, which will be shown in back which one overlaps which one. 

   3. Each component will have option to have visual effect. 

   4. They can add list of component like background that will keep on rotating like Hero Banner. Also have stati content on top. Like imafe text and so on. 

   5. The components may have videos as well. If there are list of Videos in the component, He/She will have further options to either, run the whole video, and then go to another video or state the number of seconds he want each video to run before it is rotated. If the video should be on mute or with volume. 

   6. If the video is shot as compare to the seconds mentioned for rotating, then that will be considered for that video to be rotated. 

   7. Each component added in the Banner can have list of Component that can be shown with visual effect. 

   8. Shop Owner can create multiple Banners with process. Every banner created will have to go via Approval stage. 

   9. Each banner can be viewed in Preview stage to understand how it looks like. This preview stage is available to all Logins. 

   - 10.Each Banner will have its Version maintained to 10 versions. 

   - 11.Any time user can bring older version. But will have to go through same approval process as newly created. 

   - 12.Though older version is publishing still the latest will be preserved as version and newly older version is set new value as latest version.. 

   - 13.Data in the Backend will be in encrypted format. Ensuring security of data. 

   - 14.There will be option available to shop owner, if he wish to give some space for Advertisement in his board. This section may be update based on percentage of the Banner, the side of the banner. 

   - 15.The shop Owner can also opt for Mega Advertisement where the sides will switch for a period of time, the one opted for advertisement will have banner and the major one will be considered in Advertisement. 

   - 16.When there is no Advertisement or the time set for advertisement has passed, the space will be occupied by shop Banner. 

   - 17.The Advertisement can also be shown as Popup on the Banner. 

   - 18.When shown as Popup, the size of advertisement is not relavant. 

   - 19.For Minor Advertisement, Shop Owner will have option to add the advertisement as component and charge for the same. 

   - 20.For Major Advertisement, there will be notification send to Each shop owner based on Location. The charges for each Shope will be different depending on the locality, area, and size of the banner. 

   - 21.There will be Charge management system for Advertisement, that will be flexible on shop level. Default charges will be Setup based on Locality, Area, Size of Banner. 

- v. In Order to publish a banner or creation of new Banner or Approval. There should not be need of code deployment or and kind of reset required. This should be part of web application. 

- vi. Every implementation should make it no code once set. Creation of banners will be drag and drop with approval and publish. All modification future creation are to be considered. There should be validations if banner created are for same day publish then validation need to be in hours, the hours set should not overlap each other. 

- vii. We have no ides about what can we give in Silver, Gold Platinium.. but we will start from this first. 

- viii. I need to have all type of management pages as well. Like user management, shop management banner management reporting per user can get, over all reporting considering HIPPA and PHI compliance in mind.. 

The implementation will be: 

NextJS as Frontend Application (if there is limitation to have drag drop implementation you are free to suggest , I am fine with CMS systems as well which are cheap and open source.), APS.Net core web API microservice as backend, with JWT token, Authentication and Authorization. MS SQL Server DB management. Docker and Kubernetes as deployment. I would like to first teat this all in local and then deploy it on Azure cloud. 

